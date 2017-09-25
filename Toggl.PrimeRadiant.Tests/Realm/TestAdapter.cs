using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.PrimeRadiant.Realm;

namespace Toggl.PrimeRadiant.Tests.Realm
{
    public sealed class TestAdapter : IRealmAdapter<TestModel>
    {
        private readonly List<TestModel> list = new List<TestModel>();

        public TestModel Get(long id)
            => list.Single(e => e.Id == id);

        public TestModel Create(TestModel entity)
        {
            if (list.Find(e => e.Id == entity.Id) != null)
                throw new InvalidOperationException();

            list.Add(entity);

            return entity;
        }

        public TestModel Update(long id, TestModel entity)
        {
            var index = list.FindIndex(e => e.Id == id);

            if (index == -1)
                throw new InvalidOperationException();

            list[index] = entity;

            return entity;
        }

        public IQueryable<TestModel> GetAll()
            => list.AsQueryable();

        public void Delete(long id)
        {
            var entity = Get(id);
            var worked = list.Remove(entity);
            if (worked) return;

            throw new InvalidOperationException();
        }

        public IEnumerable<(ConflictResolutionMode ResolutionMode, TestModel Entity)> BatchUpdate(
            IEnumerable<(long Id, TestModel Entity)> entities,
            Func<TestModel, TestModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TestModel> resolver)
        {
            throw new NotImplementedException();
        }
    }
}
