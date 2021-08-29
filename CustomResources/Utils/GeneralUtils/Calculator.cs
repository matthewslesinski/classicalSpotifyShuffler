using System;

namespace CustomResources.Utils.GeneralUtils
{
	public static class Calculator
	{

		public static uint ToNextMultipleOf64(uint num)
		{
			return (num + 63) & ~63U;
		}

		public static uint ToNextPowerOfTwo(uint num)
		{
			num--;
			num |= num >> 1;
			num |= num >> 2;
			num |= num >> 4;
			num |= num >> 8;
			num |= num >> 16;
			num++;
			return num;
		}

		public static int CountBitsWithHeuristic(ulong[] nums, int minPossibleAnswer, int maxPossibleAnswer)
		{
			var average = (((long)nums.Length) << 6) >> 1;

			int total = 0;
			foreach (var num in nums)
			{
				int batchSize;
				if (minPossibleAnswer - total >= average)
					batchSize = 64 - CountBitsProportionalOperations(~num);
				else if (maxPossibleAnswer - total <= average)
					batchSize = CountBitsProportionalOperations(num);
				else
					batchSize = CountBitsBoundedOperations(num);

				total += batchSize;
				average -= 32;
			}
			return total;
		}

		public static int CountBitsSimple(ulong[] nums)
		{
			int total = 0;
			foreach (var num in nums)
				total += CountBitsBoundedOperations(num);
			return total;
		}

		public static int CountBitsBoundedOperations(ulong num)
		{
			num -= (num >> 1) & 0x5555555555555555UL;
			num = (num & 0x3333333333333333UL) + ((num >> 2) & 0x3333333333333333UL);
			return (int) (unchecked(((num + (num >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
		}

		public static int CountBitsProportionalOperations(ulong num)
		{
			int total = 0;
			for (; num > 0; total++)
				num &= num - 1;

			return total;
		}
	}
}
