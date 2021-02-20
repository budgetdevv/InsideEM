using System;
using InsideEM;

namespace PooledListTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //Test1();

            Test2();
        }
        
        private static void Test1()
        {
            var List = new PooledList<int>(1);

            for (int I = 1; I <= 16; I++)
            {
                List.Add(I);
            }

            List.Remove(1);

            List.Add(2);

            List.Remove(9);

            List.Add(69);

            foreach (var x in List)
            {
                Console.WriteLine(x);
            }
        }
        
        private static void Test2()
        {
            var List = new PooledList<int>(20);

            for (int I = 1; I <= 10; I++)
            {
                List.Add(I);
            }

            foreach (var x in List)
            {
                Console.WriteLine(x);
            }
        }
    }
}
