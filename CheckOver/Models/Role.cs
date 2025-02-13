﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CheckOver.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        public virtual ICollection<Invitation> Invitations { get; set; }
        public virtual ICollection<Assignment> Assignments { get; set; }
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
        public string Name { get; set; }
    }
}