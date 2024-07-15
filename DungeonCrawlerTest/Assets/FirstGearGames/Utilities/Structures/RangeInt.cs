using FirstGearGames.Utilities.Maths;

namespace FirstGearGames.Utilities.Structures
{


    [System.Serializable]
    public struct IntRange
    {
        public IntRange(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
        /// <summary>
        /// Minimum range.
        /// </summary>
        public int Minimum;
        /// <summary>
        /// Maximum range.
        /// </summary>
        public int Maximum;

        /// <summary>
        /// Returns an exclusive random value between Minimum and Maximum.
        /// </summary>
        /// <returns></returns>
        public float RandomExclusive()
        {
            return Ints.RandomExclusiveRange(Minimum, Maximum);
        }
        /// <summary>
        /// Returns an inclusive random value between Minimum and Maximum.
        /// </summary>
        /// <returns></returns>
        public float RandomInclusive()
        {
            return Ints.RandomInclusiveRange(Minimum, Maximum);
        }
    }


}