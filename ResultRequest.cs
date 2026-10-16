using System.ComponentModel.DataAnnotations;

namespace RaceDay.RequestAndResponseModels
{
    public class ResultRequest
    {
        public TimeOnly FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int Position { get; set; }
    }
}

