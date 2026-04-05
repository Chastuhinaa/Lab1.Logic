using System;

namespace Lab1.Logic
{
    public interface ISceneRepository
    {
        bool TestConnection();
        string LoadSceneData(int sceneId);
        void SaveRenderResult(int sceneId, string result);
        void LogEvent(string message);
    }

    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base(message) { }
    }

    public class RenderManager
    {
        private readonly ISceneRepository _repository;

        public RenderManager(ISceneRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public bool ProcessAndSaveScene(int sceneId)
        {
            try
            {
                if (!_repository.TestConnection())
                {
                    _repository.LogEvent("Connection failed.");
                    return false;
                }

                string sceneData = _repository.LoadSceneData(sceneId);
                if (string.IsNullOrEmpty(sceneData))
                {
                    return false;
                }

                _repository.LogEvent($"Scene {sceneId} loaded successfully.");

                string fakeRenderOutput = $"P3\n800 600\n255\n... rendered data for {sceneData}";

                _repository.SaveRenderResult(sceneId, fakeRenderOutput);
                _repository.LogEvent($"Render saved for scene {sceneId}.");

                return true;
            }
            catch (Exception ex)
            {
                _repository.LogEvent($"Error processing scene: {ex.Message}");
                throw;
            }
        }
    }
}