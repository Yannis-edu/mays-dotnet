﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Epsic.Info3e.Mays.DbContext;
using Epsic.Info3e.Mays.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Epsic.Info3e.Mays.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly MaysDbContext _context;
        private readonly UserManager<User> _userManager;

        public CommentsController(MaysDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Comments
        [HttpGet]
        /// <summary>
        /// Gets a list of all comments
        /// </summary>
        /// <returns>A list of all comments</returns>
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments()
        {
            var comments = await _context.Comments.Include(c => c.Post).Include(c => c.Author).ToListAsync();
            return comments.Select(c => ToCommentDto(c)).ToList();
        }

        // GET: api/Comments/post/{id}
        [HttpGet("post/{postId}")]
        /// <summary>
        /// Gets a list of all comments in a post
        /// </summary>
        /// <param name="postId">Id of the post to get the comments of</param>
        /// <returns>A list of all comments in the post</returns>
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetPostComments(string postId)
        {
            var comments = await _context.Comments.Where(c => c.PostId == postId).Include(c => c.Post).Include(c => c.Author).ToListAsync();
            return comments.Select(c => ToCommentDto(c)).ToList();
        }

        // GET: api/Comments/5
        [HttpGet("{id}")]
        /// <summary>
        /// Gets a single comment
        /// </summary>
        /// <param name="id">Id of the comment to get</param>
        /// <returns>The comment, or a notfound</returns>
        public async Task<ActionResult<CommentDto>> GetComment(string id)
        {
            var comment = await _context.Comments.Include(c => c.Post).Include(c => c.Author).FirstAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            return ToCommentDto(comment);
        }

        // PUT: api/Comments/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "user,premium,admin")]
        /// <summary>
        /// Updates a comment
        /// </summary>
        /// <param name="comment">Comment to update</param>
        /// <returns>Badrequest if the comment does not exist, nocontent on edit, forbid if different author, or an exception</returns>
        public async Task<IActionResult> PutComment(string id, CommentUpdate comment)
        {
            comment.Id = id;
            if (!CommentExists(comment.Id))
            {
                return BadRequest();
            }
            var originalComment = await _context.Comments.Include(c => c.Author).FirstAsync(c => c.Id == comment.Id);
            if (getCurrentUserId() != originalComment.Author.Id)
            {
                return Forbid();
            }

            originalComment.Content = comment.Content;
            originalComment.IsSpoiler = comment.IsSpoiler;
            _context.Entry(originalComment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        // POST: api/Comments
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "user,premium,admin")]
        /// <summary>
        /// Adds a comment
        /// </summary>
        /// <param name="comment">Comment to add</param>
        /// <returns>Createdataction on success</returns>
        public async Task<ActionResult<Comment>> PostComment(Comment comment)
        {
            comment.Author = await _userManager.FindByIdAsync(User.Claims.FirstOrDefault(x => x.Type == "Id").Value);
            comment.Date = DateTime.Now;
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetComment", new { id = comment.Id }, comment);
        }

        // DELETE: api/Comments/5
        [HttpDelete("{id}")]
        /// <summary>
        /// Deletes a comment
        /// </summary>
        /// <param name="id">Id of the comment to delete</param>
        /// <returns>Notfound if the comment does not exist, forbid if not the author, nocontent if it no longer exists</returns>
        public async Task<IActionResult> DeleteComment(string id)
        {
            var comment = await _context.Comments.Include(c => c.Author).FirstAsync(c => c.Id == id);

            if (getCurrentUserId() != comment.Author.Id)
            {
                return Forbid();
            }

            if (comment == null)
            {
                return NotFound();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Checks whether a comment exists
        /// </summary>
        /// <param name="id">Id of the comment to check for</param>
        /// <returns>True if it exists, false otherwise</returns>
        private bool CommentExists(string id)
        {
            return _context.Comments.Any(e => e.Id == id);
        }

        /// <summary>
        /// Returns the id of the current user
        /// </summary>
        /// <returns>The id of the current user if exists, or null</returns>
        private string getCurrentUserId()
        {
            if (User.Claims.Any(x => x.Type == "Id"))
            {
                return User.Claims.FirstOrDefault(x => x.Type == "Id").Value;
            }
            return null;
        }

        private CommentDto ToCommentDto(Comment comment)
        {
            return new CommentDto
            {
                Id = comment.Id,
                Date = comment.Date,
                Post = new PostDto
                {
                    Id = comment?.Post?.Id,
                    Title = comment?.Post?.Title,
                    Date = comment.Post.Date,
                    Content = comment?.Post?.Content,
                    FilePath = comment?.Post?.FilePath,
                    FileType = comment?.Post?.FileType,
                    IsSpoiler = comment.Post.IsSpoiler
                },
                Author = new UserDto {
                    UserName = comment?.Author?.UserName,
                    Avatar = comment?.Author?.Avatar
                },
                Content = comment.Content,
                IsSpoiler = comment.IsSpoiler
            };
        }
    }
}
