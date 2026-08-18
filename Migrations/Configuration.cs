using System.Data.Entity.Migrations;

namespace AlarmApp.Migrations
{
    // Bu sınıf EF6'nın "Code-First Automatic Migrations" özelliğini açar.
    
    internal sealed class Configuration : DbMigrationsConfiguration<AlarmApp.AlarmDbContext>
    {
        public Configuration()
        {
            
            AutomaticMigrationsEnabled = true;

            
            AutomaticMigrationDataLossAllowed = true;
        }
    }
}
