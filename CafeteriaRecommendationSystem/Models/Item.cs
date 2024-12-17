using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CafeteriaRecommendationSystem.Models
{
    public class Item
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public bool AvailabilityStatus { get; set; }
        public int MealTypeId { get; set; }
        public string DietPreference { get; set; }
        public string SpiceLevel { get; set; }
        public string FoodPreference { get; set; }
        public string SweetTooth { get; set; }
    }
}
