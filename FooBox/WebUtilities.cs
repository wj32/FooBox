using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FooBox
{
    public static class WebUtilities
    {
        public static void LockTableExclusive(this System.Data.Entity.Database database, string tableName)
        {
            database.ExecuteSqlCommand("SELECT TOP 1 Id FROM " + tableName + " WITH (TABLOCKX, HOLDLOCK)");
        }
    }
}