using System.Data;
using AuthAPI.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly DataContextDapper _dapper;

        public CommentController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("{postId}")]
        public IActionResult GetComments(int postId)
        {
            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@PostId", postId, DbType.Int32);

            string sql = "EXEC Auth.usp_GetComments @PostId";

            var comments = _dapper.LoadDataWithParameters<dynamic>(sql, sqlParameters);
            return Ok(comments);
        }

        [HttpPost("Create/{postId}")]
        public IActionResult CreateComment(int postId, [FromBody] string commentContent)
        {
            int userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");

            if (userId == 0) return Unauthorized();

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@PostId", postId, DbType.Int32);
            sqlParameters.Add("@UserId", userId, DbType.Int32);
            sqlParameters.Add("@CommentContent", commentContent, DbType.String);

            string sql = "EXEC Auth.usp_CreateComment @PostId, @UserId, @CommentContent";

            try
            {
                _dapper.ExecuteSqlWithParameters(sql, sqlParameters);

                return Ok("Comment added successfully!");
            }
            catch (Exception ex)
            {
                // If the PostId doesn't exist or SQL fails, it lands here
                return BadRequest("Failed to add comment. Make sure the Post ID exists! " + ex.Message);
            }
        }

        [HttpPut("Edit/{commentId}")]
        public IActionResult EditComment(int commentId, [FromBody] string commentContent)
        {
            int userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            if (userId == 0) return Unauthorized();

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@CommentId", commentId, DbType.Int32);
            sqlParameters.Add("@UserId", userId, DbType.Int32);
            sqlParameters.Add("@CommentContent", commentContent, DbType.String);

            string sql = "EXEC Auth.usp_EditComment @CommentId, @UserId, @CommentContent";

            int rowsAffected = _dapper.LoadDataSingleWithParameters<int>(sql, sqlParameters);

            if (rowsAffected > 0)
                return Ok("Comment updated successfully!");

            return BadRequest("Failed to update comment. It may not exist, or you do not have permission.");
        }

        [HttpDelete("Delete/{commentId}")]
        public IActionResult DeleteComment(int commentId)
        {
            int userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            if (userId == 0) return Unauthorized();

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@CommentId", commentId, DbType.Int32);
            sqlParameters.Add("@UserId", userId, DbType.Int32);

            string sql = "EXEC Auth.usp_DeleteComment @CommentId, @UserId";

            int rowsAffected = _dapper.LoadDataSingleWithParameters<int>(sql, sqlParameters);

            if (rowsAffected > 0)
                return Ok("Comment deleted successfully!");

            return BadRequest("Failed to delete comment. It may not exist, or you do not have permission.");
        }
    }
}