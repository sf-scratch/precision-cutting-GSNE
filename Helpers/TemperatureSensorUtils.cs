using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Entities;
using 精密切割系统.Model.common;
using 精密切割系统.PubSubEvent;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Helpers
{
    internal class TemperatureSensorUtils
    {
        public static async Task StartRecordingAsync()
        {
            List<TemperatureSensorEntity> temperatureSensors = await SqlHelper.TableAsync<TemperatureSensorEntity>().ToListAsync();
            var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync())
            {
                try
                {
                    DateTime dateTime = DateTime.Now;
                    var temperatures = await PlcControl.tagControl.wholeDevice.GetTemperatureSensorsAsync();
                    if (temperatures is not null && temperatureSensors.Count == temperatures.Length)
                    {
                        for (int i = 0; i < temperatureSensors.Count; i++)
                        {
                            TemperatureSensorEntity sensor = temperatureSensors[i];
                            float temperature = temperatures[i];
                            var record = new TemperatureLogEntity
                            {
                                SensorId = sensor.Id,
                                CreatedAt = dateTime,
                                Temperature = temperature
                            };
                            await SqlHelper.AddAsync(record);
                        }
                    }
                }
                catch (Exception)
                {
                }
                //Random random = new Random();
                //for (int i = 0; i < temperatureSensors.Count; i++)
                //{
                //    TemperatureSensorEntity sensor = temperatureSensors[i];
                //    float temperature = random.Next(1, 101);
                //    var record = new TemperatureLogEntity
                //    {
                //        SensorId = sensor.Id,
                //        CreatedAt = dateTime,
                //        Temperature = temperature
                //    };
                //    await SqlHelper.AddAsync(record);
                //}
            }
        }
    }
}