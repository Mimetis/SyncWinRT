#if ( WINDOWS_PHONE || NETFX_CORE)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Microsoft.Synchronization.ClientServices.Common
{
    internal static class StorageExtensions
    {
        /// <summary>
        /// Methode d'extension vérifiant l'existance d'un fichier
        /// </summary>
        public async static Task<bool> FileExistsAsync(this StorageFolder folder, string name)
        {
            try
            {
                var files = await folder.GetFilesAsync();
                return files.Any(f => f.Name == name);

            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Methode d'extension vérifiant l'existance d'un Répertoire
        /// </summary>
        public async static Task<bool> FolderExistsAsync(this StorageFolder folder, string name)
        {
            var folders = await folder.GetFoldersAsync();

            return folders.Any(f => f.Name == name);
        }
    }
}
#endif