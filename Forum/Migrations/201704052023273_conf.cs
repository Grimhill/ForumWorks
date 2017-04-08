namespace Forum.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class conf : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "OnlineStatus", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "OnlineStatus");
        }
    }
}
