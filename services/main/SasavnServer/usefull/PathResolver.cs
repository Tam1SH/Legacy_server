namespace SasavnServer.usefull
{
	public class PathResolver
    {
        private PathString BasePathToFiles;
        private Uri BaseUrlToFiles;

        public PathResolver(PathString pathToFiles, Uri urlToFiles)
        {
            BasePathToFiles = pathToFiles;
            BaseUrlToFiles = urlToFiles;
        }

        public Uri PathSegmentToUrl(PathString path)
        {
            return new Uri($"{BaseUrlToFiles.Scheme}://{BaseUrlToFiles.Host}{BaseUrlToFiles.LocalPath}{path}");
        }

		public Uri AbsoluteUri(Uri? uri = null) {
			if (uri == null)
                return BaseUrlToFiles;

			return new Uri(BaseUrlToFiles.ToString() +  uri.ToString());
		}
        public string AbsolutePath(PathString? path = null)
        {
            if (path == null)
                return BasePathToFiles;
            
			return new PathString($"{BasePathToFiles}{path}".Replace("//", "/"))
				.Value!
				.Replace("%20", " ");
			
        }

    }
}
