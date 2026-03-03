using System.Data;
using AuthAPI.Data;
using AuthAPI.Dtos;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PostController : ControllerBase
    {
        private readonly DataContextDapper _dapper;

        public PostController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("Feed")]
        public IActionResult GetFeed()
        {
            string sql = "EXEC Auth.usp_GetFeed";
            var posts = _dapper.LoadData<PostDisplayDto>(sql);
            return Ok(posts);
        }

        [HttpPost("Create")]
        public IActionResult CreatePost([FromBody] string postContent)
        {
            // Extract the UserId from the JWT Token claims
            int userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");

            if (userId == 0) return Unauthorized();

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@UserId", userId, DbType.Int32);
            sqlParameters.Add("@PostContent", postContent, DbType.String);

            string sql = "EXEC Auth.usp_CreatePost @UserId, @PostContent";

            try
            {
                // Use LoadDataSingleWithParameters to grab the PostId returned by SCOPE_IDENTITY()
                int newPostId = _dapper.LoadDataSingleWithParameters<int>(sql, sqlParameters);

                // Return a 200 OK with the new ID
                return Ok(new { Message = "Post created successfully!", PostId = newPostId });
            }
            catch (Exception)
            {
                // If SQL actually fails, we will catch it here
                return BadRequest("Failed to create post.");
            }
        }

        [HttpPut("Edit/{postId}")]
        public IActionResult EditPost(int postId, [FromBody] string postContent)
        {
            int userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            if (userId == 0) return Unauthorized();

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@PostId", postId, DbType.Int32);
            sqlParameters.Add("@UserId", userId, DbType.Int32);
            sqlParameters.Add("@PostContent", postContent, DbType.String);

            string sql = "EXEC Auth.usp_EditPost @PostId, @UserId, @PostContent";

            int rowsAffected = _dapper.LoadDataSingleWithParameters<int>(sql, sqlParameters);

            if (rowsAffected > 0)
                return Ok("Post updated successfully!");

            return BadRequest("Failed to update post. It may not exist, or you do not have permission.");
        }

        [HttpDelete("Delete/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            int userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            if (userId == 0) return Unauthorized();

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@PostId", postId, DbType.Int32);
            sqlParameters.Add("@UserId", userId, DbType.Int32);

            string sql = "EXEC Auth.usp_DeletePost @PostId, @UserId";

            int rowsAffected = _dapper.LoadDataSingleWithParameters<int>(sql, sqlParameters);

            if (rowsAffected > 0)
                return Ok("Post deleted successfully!");

            return BadRequest("Failed to delete post. It may not exist, or you do not have permission.");
        }
    }
}