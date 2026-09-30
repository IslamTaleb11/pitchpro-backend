using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.StaffCategory;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class StaffCategoryProvider
    {
        public static void AddStaffCategories(StaffCategoryRegistrationSaveDTO dto)
        {
            // 1. Create a DataTable to hold the IDs
            // This MUST match the structure of your 'dbo.IntegerList' SQL type
            DataTable categoryTable = new DataTable();
            categoryTable.Columns.Add("ItemValue", typeof(int));

            // 2. Fill the DataTable with the array values
            if (dto.Categories != null)
            {
                foreach (int id in dto.Categories)
                {
                    categoryTable.Rows.Add(id);
                }
            }

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                // We use the Stored Procedure here because it's the 
                // easiest way to pass a TVP (the DataTable).
                using (SqlCommand cmd = new SqlCommand("sp_AddStaffCategories", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add the regular StaffID parameter
                    cmd.Parameters.AddWithValue("@StaffID", dto.StaffID);

                    // 3. Add the Table-Valued Parameter (the "Bucket" of IDs)
                    SqlParameter tvpParam = cmd.Parameters.AddWithValue("@CategoryIDs", categoryTable);

                    // This tells SQL to treat the DataTable as a structured TVP
                    tvpParam.SqlDbType = SqlDbType.Structured;

                    // This MUST match the name of the TYPE you created in SSMS
                    tvpParam.TypeName = "dbo.IntegerList";

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
