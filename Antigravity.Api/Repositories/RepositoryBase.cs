using Microsoft.Extensions.Configuration;

namespace Antigravity.Api.Repositories
{
    public abstract class RepositoryBase
    {
        protected readonly string _connectionString;

        public RepositoryBase(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
    }
}
