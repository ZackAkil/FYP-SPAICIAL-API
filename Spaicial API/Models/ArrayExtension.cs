﻿
namespace Spaicial_API.Models
{

    public static class ArrayExtension
    {

        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

    }

}