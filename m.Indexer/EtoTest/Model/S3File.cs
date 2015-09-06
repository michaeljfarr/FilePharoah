using System;

namespace EtoTest.Model
{
    public class S3File
    {
        public string Key { get; set; }
        public int AgeInDays { get; set; }
        public String AgeInDaysString => AgeInDays.ToString();
        public bool? Check { get; set; }
    }
}