using NUnit.Framework;
using Moq;
using Lab1.Logic;
using System;

namespace Lab1.Tests
{
    [TestFixture]
    public class RenderManagerMoqTests
    {
        private Mock<ISceneRepository> _mockRepo;
        private RenderManager _renderManager;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<ISceneRepository>();
            _renderManager = new RenderManager(_mockRepo.Object);
        }

        [Test]
        public void ProcessAndSaveScene_WhenLoadThrowsException_LogsErrorAndThrows()
        {
            int sceneId = 99;
            _mockRepo.Setup(r => r.TestConnection()).Returns(true);

            _mockRepo.Setup(r => r.LoadSceneData(sceneId))
                     .Throws(new RepositoryException("Database failure"));

            var ex = Assert.Throws<RepositoryException>(() =>
                _renderManager.ProcessAndSaveScene(sceneId));

            Assert.That(ex.Message, Is.EqualTo("Database failure"));
            _mockRepo.Verify(r => r.LogEvent("Error processing scene: Database failure"), Times.Once);
        }

        [Test]
        public void ProcessAndSaveScene_ValidScene_CallsMethodsInSpecificOrder()
        {
            int sceneId = 1;
            var sequence = new MockSequence();

            _mockRepo.InSequence(sequence).Setup(r => r.TestConnection()).Returns(true);
            _mockRepo.InSequence(sequence).Setup(r => r.LoadSceneData(sceneId)).Returns("SceneData1");
            _mockRepo.InSequence(sequence).Setup(r => r.LogEvent(It.IsAny<string>()));
            _mockRepo.InSequence(sequence).Setup(r => r.SaveRenderResult(sceneId, It.IsAny<string>()));
            _mockRepo.InSequence(sequence).Setup(r => r.LogEvent(It.IsAny<string>()));

            bool result = _renderManager.ProcessAndSaveScene(sceneId);

            Assert.That(result, Is.True);

            _mockRepo.Verify(r => r.TestConnection(), Times.Exactly(1));
            _mockRepo.Verify(r => r.LoadSceneData(sceneId), Times.Exactly(1));
            _mockRepo.Verify(r => r.SaveRenderResult(sceneId, It.IsAny<string>()), Times.Exactly(1));
            _mockRepo.Verify(r => r.LogEvent(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void ProcessAndSaveScene_WhenSaveThrowsException_LogsErrorAndThrows()
        {
            int sceneId = -5;

            _mockRepo.Setup(r => r.TestConnection()).Returns(true);
            _mockRepo.Setup(r => r.LoadSceneData(sceneId)).Returns("SomeScene");

            _mockRepo.Setup(r => r.SaveRenderResult(sceneId, It.IsAny<string>()))
                     .Throws(new ArgumentException("Invalid scene ID"));

            var ex = Assert.Throws<ArgumentException>(() =>
                _renderManager.ProcessAndSaveScene(sceneId));

            Assert.That(ex.Message, Is.EqualTo("Invalid scene ID"));

            _mockRepo.Verify(r => r.LogEvent(
                It.Is<string>(msg => msg.Contains("Error processing scene"))),
                Times.Once);
        }

        [Test]
        public void TestConnection_MultipleCalls_ReturnsDifferentValuesConsecutively()
        {
            _mockRepo.SetupSequence(r => r.TestConnection())
                     .Returns(false)
                     .Returns(true)
                     .Returns(false);

            bool firstAttempt = _renderManager.ProcessAndSaveScene(1);
            Assert.That(firstAttempt, Is.False);

            _mockRepo.Setup(r => r.LoadSceneData(1)).Returns("Valid Data");
            bool secondAttempt = _renderManager.ProcessAndSaveScene(1);
            Assert.That(secondAttempt, Is.True);

            bool thirdAttempt = _renderManager.ProcessAndSaveScene(1);
            Assert.That(thirdAttempt, Is.False);
        }
    }
}