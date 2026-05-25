using System;
using System.Collections.Generic;

namespace TravelHub.DTO
{
    public class WeatherForecastDto
    {
        public int DestinationID { get; set; }
        public string DestinationName { get; set; } = string.Empty;
        public string? OpenWeatherMapCityID { get; set; }
        public List<DailyWeatherDto> Forecasts { get; set; } = new List<DailyWeatherDto>();
    }

    public class DailyWeatherDto
    {
        public DateTime Date { get; set; }
        public double TemperatureCelsius { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string IconURL { get; set; } = string.Empty;
        public int HumidityPercentage { get; set; }
    }
}
