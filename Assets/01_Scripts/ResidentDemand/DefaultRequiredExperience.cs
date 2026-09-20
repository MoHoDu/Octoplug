using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public static class DefaultRequiredExperience
    {
        public static RequiredExperienceTable Create()
        {
            return RequiredExperienceImporter.FromAuthoringRows(new[]
            {
                Row(1, 20),
                Row(2, 30),
                Row(3, 40),
                Row(4, 50),
                Row(5, 60),
                Row(6, 70),
                Row(7, 80),
                Row(8, 90),
                Row(9, 100),
                Row(10, 110),
                Row(11, 120),
                Row(12, 130),
                Row(13, 140),
                Row(14, 150),
                Row(15, 160)
            });
        }

        private static RequiredExperienceAuthoringRow Row(int roomCount, int requiredExperience)
        {
            return new RequiredExperienceAuthoringRow(roomCount, requiredExperience);
        }
    }
}
