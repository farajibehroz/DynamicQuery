using Infrastructure;
using Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ConsoleAppTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var repository = new AlbumRepository();

            var result = repository.GetAll().Filter(new AlbumFilter() {  Artist = "3" });
            WriteAll(result);

            result = repository.GetAll().Where<Album>(ExpressionType.Power, "Singer.Age", new List<int> { 10, 28 });
            WriteAll(result);

            result = repository.GetAll().Where<Album>(ExpressionType.Power, "Singer.FirstName", "A");
            WriteAll(result);

            Console.ReadKey();

            result = result.OrderBy("Singer.Age");
            WriteAll(result);

            result = repository.GetAll().Where<Album>(ExpressionType.NotEqual, "Singer.FirstName", "Ali");
            WriteAll(result);

            var result2 = result.OrderBy("Quantity");
            WriteAll(result2);

            result = repository.GetAll().Where<Album>(ExpressionType.Equal, "Singer.Age", 10);
            var result1 = result.OrderBy("Singer.Age");
            WriteAll(result1);

            result = repository.GetAll().Where<Album>(ExpressionType.GreaterThanOrEqual, "Quantity", 10);
            WriteAll(result);
            Console.ReadKey();
        }


        private static void WriteAll(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                Write(item);
            }
            Console.WriteLine();
        }

        private static void Write(object item)
        {
            Console.WriteLine(item);
        }
    }
}
