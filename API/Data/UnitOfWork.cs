using API.Interfaces;
using AutoMapper;
using System.Threading.Tasks;

namespace API.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        public UnitOfWork(DataContext dataContext,
            IMapper mapper)
        {
            this._dataContext = dataContext;
            this._mapper = mapper;
        }

        public IUserRepository UserRepository => new UserRepository(_dataContext, _mapper);
        public IMessageRepository MessageRepository => new MessageRepository(_dataContext, _mapper);
        public ILikesRepository LikesRepository => new LikesRepository(_dataContext);
        public async Task<bool> Complete()
        {
            return await _dataContext.SaveChangesAsync() > 0;
        }

        public bool HasChanges()
        {
            return _dataContext.ChangeTracker.HasChanges();
        }
    }
}