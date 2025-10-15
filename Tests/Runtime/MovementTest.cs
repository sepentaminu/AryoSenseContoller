using NUnit.Framework;
using UnityEngine;

namespace Aryosense.Controller.Tests
{
    public class MovementTest
    {
        [Test]
        public void DefaultSpeed_ShouldBePositive()
        {
            var config = ScriptableObject.CreateInstance<MovementConfig>();
            Assert.Greater(config.MoveSpeed, 0f);
        }
    }
}
