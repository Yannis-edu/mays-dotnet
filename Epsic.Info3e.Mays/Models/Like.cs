﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Epsic.Info3e.Mays.Models
{
    public class Like
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public string Id { get; set; }
        [JsonIgnore]
        public string UserId { get; set; }
        [JsonIgnore]
        public IdentityUser User { get; set; }
        [Required]
        public string PostId { get; set; }
        [JsonIgnore]
        public Post Post { get; set; }
    }
}
