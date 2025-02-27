﻿using System;

namespace UberStrok.Core.Common
{
    public static class MathUtils
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }

        public static int CeilToInt(float f)
        {
            return (int)Math.Ceiling((double)f);
        }

        public static int Max(int a, int b)
        {
            return (a <= b) ? b : a;
        }
    }
}
