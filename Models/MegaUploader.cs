using CG.Web.MegaApiClient;

namespace Backend_guichet_unique.Models
{
	public class MegaUploader
	{
		private readonly MegaApiClient _client;

		public MegaUploader()
		{
			_client = new MegaApiClient();
		}

		public async Task LoginAsync(string email, string password)
		{
			await _client.LoginAsync(email, password);
		}

		public async Task<string> UploadFileAsync(string filePath, string remoteFolderName)
		{
			var nodes = await _client.GetNodesAsync();
			var root = nodes.Single(n => n.Type == NodeType.Root);
			var remoteFolder = nodes.FirstOrDefault(n => n.Type == NodeType.Directory && n.Name == remoteFolderName);

			if (remoteFolder == null)
			{
				remoteFolder = await _client.CreateFolderAsync(remoteFolderName, root);
			}

			using (var stream = new FileStream(filePath, FileMode.Open))
			{
				var fileName = Path.GetFileName(filePath); // Obtenir le nom du fichier
				var node = await _client.UploadAsync(stream, fileName, remoteFolder); // Utiliser le nom du fichier
				return node.Id;
			}
		}

		public async Task<string> GetShareableLinkAsync(string fileId)
		{
			var node = (await _client.GetNodesAsync()).Single(n => n.Id == fileId);
			Uri uri = await _client.GetDownloadLinkAsync(node);
			return uri.ToString();
		}

		public async Task LogoutAsync()
		{
			await _client.LogoutAsync();
		}
	}
}
