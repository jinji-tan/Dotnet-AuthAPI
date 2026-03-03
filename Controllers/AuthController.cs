using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthAPI.Data;
using AuthAPI.Dtos;
using AuthAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DataContextDapper _dapper;
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _config = config;
        }

        [HttpPost("Register")]
        public IActionResult Register(UserForRegistration userForRegistration)
        {
            if (userForRegistration.Password != userForRegistration.PasswordConfirm)
            {
                return BadRequest("Passwords do not match!");
            }

            byte[] passwordSalt = new byte[128 / 8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetNonZeroBytes(passwordSalt);
            }

            byte[] passwordHash = GetPasswordHash(userForRegistration.Password, passwordSalt);

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@FirstName", userForRegistration.FirstName, DbType.String);
            sqlParameters.Add("@LastName", userForRegistration.LastName, DbType.String);
            sqlParameters.Add("@Email", userForRegistration.Email, DbType.String);
            sqlParameters.Add("@PasswordHash", passwordHash, DbType.Binary);
            sqlParameters.Add("@PasswordSalt", passwordSalt, DbType.Binary);

            string sql = "EXEC Auth.usp_RegisterUser @FirstName, @LastName, @Email, @PasswordHash, @PasswordSalt";

            try
            {
                _dapper.ExecuteSqlWithParameters(sql, sqlParameters);
                return Ok("User successfully registered!");
            }
            catch (Exception)
            {
                return BadRequest("Registration failed. Email might already exist.");
            }
        }

        [HttpPost("Login")]
        public IActionResult Login(UserForLoginDto userForLogin)
        {
            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@Email", userForLogin.Email, DbType.String);

            string sql = "EXEC Auth.usp_GetCredentialsByEmail @Email";

            AuthCredentials? user = _dapper.LoadDataSingleWithParameters<AuthCredentials>(sql, sqlParameters);

            if (user == null)
            {
                return StatusCode(401, "Incorrect Email or Password");
            }

            byte[] passwordHash = GetPasswordHash(userForLogin.Password, user.PasswordSalt);

            for (int i = 0; i < passwordHash.Length; i++)
            {
                if (passwordHash[i] != user.PasswordHash[i])
                {
                    return StatusCode(401, "Incorrect Email or Password");
                }
            }

            return Ok(new Dictionary<string, string> {
                {"token", CreateToken(user.UserId)}
            });
        }

        private byte[] GetPasswordHash(string password, byte[] passwordSalt)
        {
            string passwordSaltPlusString = _config.GetSection("AppSettings:PasswordKey").Value +
                    Convert.ToBase64String(passwordSalt);

            return KeyDerivation.Pbkdf2(
                password: password,
                salt: Encoding.ASCII.GetBytes(passwordSaltPlusString),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
            );
        }

        private string CreateToken(int userId)
        {
            Claim[] claims = new Claim[] {
                new Claim("userId", userId.ToString())
            };

            string? tokenKeyString = _config.GetSection("AppSettings:TokenKey").Value;
            SymmetricSecurityKey tokenKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(tokenKeyString ?? "")
            );

            SigningCredentials credentials = new SigningCredentials(
                tokenKey, SecurityAlgorithms.HmacSha512Signature
            );

            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = credentials,
                Expires = DateTime.Now.AddDays(1)
            };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken token = tokenHandler.CreateToken(descriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}