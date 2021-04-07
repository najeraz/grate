﻿using FluentAssertions;
using moo.Configuration;
using NUnit.Framework;

namespace moo.unittests
{
    [TestFixture]
    public class MooConfiguration_
    {
        [Test]
        public void Uses_ConnectionString_with_master_db_if_adminConnectionString_is_not_set()
        {
            var cfg = new MooConfiguration()
                {ConnectionString = "Data source=Monomonojono;Initial Catalog=Øyenbryn;Lotsastuff"};

            cfg.AdminConnectionString.Should().Be("Data source=Monomonojono;Initial Catalog=master;Lotsastuff");

        }
        
    }
}