using System.Collections.Generic;
using System.Linq;

namespace Repository
{
    public class AlbumRepository : IRepository<Album>
    {
        private static readonly Singer singerOne = new Singer() { FirstName = "Ali", LastName = "Mohammadi", Age = 10 };
        private static readonly Singer singerTwo = new Singer() { FirstName = "Behroz", LastName = "Faraji", Age = 28 };
        private static readonly Singer singerThree = new Singer() { FirstName = "Hossein", LastName = "Zakizadeh", Age = 31 };
        private static readonly Singer singerFour = new Singer() { FirstName = "Hesam", LastName = "Akbari", Age = 30 };

        private readonly List<Album> list = new List<Album>
                    {
                        new Album { Quantity = 10, Artist = "Band1", Title = "Autumn",Singer=singerOne },
                        new Album { Quantity = 50, Artist = "Band2", Title = "Summer",Singer=singerFour },
                        new Album { Quantity = 43, Artist = "Band3", Title = "Absent" ,Singer=singerThree},
                        new Album { Quantity = 32, Artist = "Band3", Title = "Sprint" ,Singer=singerTwo},
                        new Album { Quantity = 65, Artist = "Band3", Title = "Sprint" ,Singer=singerOne},
                        new Album { Quantity = 7, Artist = "Bad4", Title = "Sprint" ,Singer=singerTwo},
                        new Album { Quantity = 12, Artist = "Band5", Title = "Fall" ,Singer=singerThree},
                    };
        public IQueryable<Album> GetAll()
        {
            return list.AsQueryable();
        }
    }

}
