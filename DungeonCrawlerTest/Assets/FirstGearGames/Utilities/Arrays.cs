using System.Collections.Generic;

namespace FirstGearGames.Utilities.Objects
{

    public static class Arrays
    {
        /// <summary>
        /// Adds an entry to a list if it does not exist already.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name=""></param>
        /// <param name="value"></param>
        public static void AddUnique<T>(this List<T> list, object value)
        {
            if (!list.Contains((T)value))
                list.Add((T)value);
        }

    }


}