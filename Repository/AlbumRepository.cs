using System.Collections.Generic;
using System.Linq;

namespace Repository
{
    public class AlbumRepository : IRepository<Album>
    {
        private static readonly Singer singerOne = new Singer() { FirstName = "Ali", LastName = "Mohammadi", Age = 10 };
        private static readonly Singer singerTwo = new Singer() { FirstName = "Behroz", LastName = "Faraji", Age = 28 };
        private static readonly Singer singerThree = new Singer() { FirstName = "Hossein", LastName = "Zakizadeh", Age = 31 };
        private static readonly Singer singerFour = new Singer() { FirstName = "Hesam", LastName = "Akbar", Age = 30 };

        private readonly List<Album> list = new List<Album>
                    {
                        new Album { Quantity = 10, Artist = "Betontod", Title = "Revolution",Singer=singerOne },
                        new Album { Quantity = 50, Artist = "The Dangerous Summer", Title = "The Dangerous Summer",Singer=singerFour },
                        new Album { Quantity = 43, Artist = "Depeche Mode", Title = "Spirit" ,Singer=singerThree},
                        new Album { Quantity = 32, Artist = "Depeche Mode", Title = "Spirit" ,Singer=singerTwo},
                        new Album { Quantity = 65, Artist = "Depeche Mode", Title = "Spirit" ,Singer=singerOne},
                        new Album { Quantity = 7, Artist = "Depeche Mode", Title = "Spirit" ,Singer=singerTwo},
                        new Album { Quantity = 12, Artist = "Depeche Mode", Title = "Spirit" ,Singer=singerThree},
                    };
        public IQueryable<Album> GetAll()
        {
            return list.AsQueryable();
        }
    }

}
