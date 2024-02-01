using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib.Helper
{
    public static class ByteExtensions
    {
        public static byte[][] ToJaggedArray(this byte[] qubicArray, long numberOfColumns)
        {
            var numberOfRows = qubicArray.Length / numberOfColumns;

            byte[][] jaggedArray = new byte[numberOfRows][];
            int row = -1;
            int col = 0;
            for (int i = 0; i < qubicArray.Length; i++)
            {
                if (i % numberOfColumns == 0)
                {
                    row++;
                    jaggedArray[row] = new byte[numberOfColumns];
                    col = 0;
                }

                jaggedArray[row][col] = qubicArray[i];
                col++;
            }
            return jaggedArray;
        }
        /// <summary>
        /// converts a c# two dimensional array [][] to a byte[] arrray to have
        /// the memory aligned correct for interoperability.
        /// </summary>
        /// <param name="twoDimArray"></param>
        /// <param name="rowLimit">if the [][] array is not filled you can limit the rows it should convert</param>
        /// <returns></returns>
        public static byte[] ToQubicArray(this byte[][] twoDimArray, int? rowLimit = null, int? fixedColumnLength = null)
        {
            var output = new byte[twoDimArray.Length * twoDimArray[0].Length];
            if (rowLimit == null)
            {
                rowLimit = twoDimArray.Length / twoDimArray[0].Length;
            }
            if(fixedColumnLength == null)
            {
                fixedColumnLength = twoDimArray[0].Length;
            }
            int index = 0;
            for (int i = 0; i < rowLimit; i++)
            {
                for (int j = 0; j < fixedColumnLength; j++)
                {
                    if(j > twoDimArray[i].Length)
                        output[index++] = 0; // fill empty data with 0
                    else
                        output[index++] = twoDimArray[i][j];
                }
            }

            return output;
        }


        /// <summary>
        /// set a ternary flag and returns the previous value
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="pos"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int SetTernaryFlag(this BitArray arr, int pos, int value)
        {
            var prevValue = arr[pos] ? 2 : arr[pos + 1] ? 1 : 0;
            if (value == 2)
            {
                arr[pos] = true;
            }
            else if (value == 1)
            {
                arr[pos + 1] = true;
            }
            else
            {
                arr[pos] = false;
                arr[pos + 1] = false;
            }
            return prevValue;
        }
    }
}
