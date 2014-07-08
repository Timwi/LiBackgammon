namespace LiBackgammon.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Games",
                c => new
                    {
                        PublicID = c.String(nullable: false, maxLength: 128),
                        WhiteToken = c.String(),
                        BlackToken = c.String(),
                        WhitePlayerID = c.Int(),
                        BlackPlayerID = c.Int(),
                        InitialPosition = c.String(),
                        Moves = c.String(),
                    })
                .PrimaryKey(t => t.PublicID);
        }
        
        public override void Down()
        {
            DropTable("dbo.Games");
        }
    }
}
