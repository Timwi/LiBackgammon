﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public sealed class Db : DbContext
    {
        public Db()
        {
            // This is false by default, but it's very important to set this to true so we can use
            // LINQ to Entities with WHERE clauses with comparisons on variables that may be null.
            // (Without it, comparisons are translated to e.g. "<> NULL" (wrong!) instead of "IS NOT NULL".)
            ((IObjectContextAdapter) this).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = true;
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Style> Styles { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<UserSession> Sessions { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
