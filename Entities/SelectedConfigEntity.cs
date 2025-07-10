using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    [Table("selected_config_table")]
    public class SelectedConfigEntity
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }

        [Column("selected_config_id")]
        public long SelectedConfigId { get; set; }

        public static async Task<long> GetCurrentSelectedConfigIdAsync(SQLiteAsyncConnection connection)
        {
            return (await GetCurrentSelectedConfigAsync(connection)).SelectedConfigId;
        }

        public static async Task SetCurrentSelectedConfigIdAsync(SQLiteAsyncConnection connection, long selectedConfigId)
        {
            SelectedConfigEntity selectedConfig = await GetCurrentSelectedConfigAsync(connection);
            selectedConfig.SelectedConfigId = selectedConfigId;
            await connection.UpdateAsync(selectedConfig);
        }

        private static async Task<SelectedConfigEntity> GetCurrentSelectedConfigAsync(SQLiteAsyncConnection connection)
        {
            List<SelectedConfigEntity> selectedConfigs = await connection.Table<SelectedConfigEntity>().ToListAsync();
            if (selectedConfigs.Count == 0)
            {
                SelectedConfigEntity selectedConfigEntity = new SelectedConfigEntity();
                await connection.InsertAsync(selectedConfigEntity);
                return selectedConfigEntity;
            }
            else
            {
                return selectedConfigs.First();
            }
        }
    }
}
