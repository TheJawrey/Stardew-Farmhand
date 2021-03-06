﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Farmhand.API.Utilities;
using Farmhand.Logging;
using Farmhand.Registries;
using Farmhand.Registries.Containers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Farmhand.Content
{
    internal class ModXnbInjector : IContentInjector
    {
        public bool IsLoader => true;
        public bool IsInjector => false;

        private static List<Microsoft.Xna.Framework.Content.ContentManager> _modManagers;
        private readonly Dictionary<string, Texture2D> _cachedAlteredTextures = new Dictionary<string, Texture2D>();

        public bool HandlesAsset(Type type, string assetName)
        {
            var item = XnbRegistry.GetItem(assetName);
            return (item?.OwningMod?.ModState != null && item.OwningMod.ModState == ModState.Loaded);
        }

        public T Load<T>(ContentManager contentManager, string assetName)
        {
            var output = default(T);

            var item = XnbRegistry.GetItem(assetName);
            try
            {
                if (item.IsXnb)
                {
                    var currentDirectory = Path.GetDirectoryName(item.AbsoluteFilePath);
                    var modContentManager = GetContentManagerForMod(contentManager, item);
                    var relPath = modContentManager.RootDirectory + "\\";
                    if (currentDirectory != null)
                    {
                        var relRootUri = new Uri(relPath, UriKind.Absolute);
                        var fullPath = new Uri(currentDirectory, UriKind.Absolute);
                        var relUri = relRootUri.MakeRelativeUri(fullPath) + "/" + item.File;

                        Log.Verbose($"Using own asset replacement: {assetName} = {relUri}");
                        output = modContentManager.Load<T>(relUri);
                    }
                }
                else if (item.IsTexture && typeof(T) == typeof(Texture2D))
                {
                    output = (T)(LoadTexture(contentManager, assetName, item));
                }
            }
            catch (Exception ex)
            {
                Log.Exception("Error reading own file", ex);
            }
            return output;
        }

        private void LoadModManagers(ContentManager contentManager)
        {
            if (_modManagers != null) return;

            _modManagers = new List<Microsoft.Xna.Framework.Content.ContentManager>();
            foreach (var modPath in ModLoader.ModPaths)
            {
                _modManagers.Add(new Microsoft.Xna.Framework.Content.ContentManager(contentManager.ServiceProvider, modPath));
            }
        }

        private Microsoft.Xna.Framework.Content.ContentManager GetContentManagerForMod(ContentManager contentManager, ModXnb mod)
        {
            LoadModManagers(contentManager);
            return _modManagers.FirstOrDefault(n => mod.OwningMod.ModDirectory.Contains(n.RootDirectory));
        }

        private object LoadTexture(ContentManager contentManager, string assetName, ModXnb item)
        {
            var modItem = TextureRegistry.GetItem(item.Texture, item.OwningMod);
            var obj = modItem?.Texture;

            if (obj == null) return null;
            
            if (item.Destination != null)
            {
                //TODO, Error checking on this.
                //TODO, Multiple mods should be able to edit this
                var originalTexture = contentManager.LoadDirect<Texture2D>(assetName);

                string assetKey = $"{assetName}-\u2764-modified";
                if (_cachedAlteredTextures.ContainsKey(assetKey))
                {
                    obj = _cachedAlteredTextures[assetKey];
                }
                else
                {
                    var texture = TextureUtility.PatchTexture(originalTexture, obj, item.Source ?? new Rectangle(0, 0, obj.Width, obj.Height), item.Destination);
                    _cachedAlteredTextures[assetKey] = texture;
                    obj = texture;
                }
            }

            Log.Verbose($"Using own asset replacement: {assetName} = {item.OwningMod.Name}.{item.Texture}");
            return obj;
        }

        public void Inject<T>(T obj, string assetName)
        {
            throw new NotImplementedException();
        }
    }
}
