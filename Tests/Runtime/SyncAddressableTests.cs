using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AddressableTests.SyncAddressables
{
    public abstract class SyncAddressableTests : AddressablesTestFixture
    {
        protected string m_PrefabKey = "syncprefabkey";
        protected string m_InvalidKey = "notarealkey";
        protected string m_SceneKey = "syncscenekey";

#if UNITY_EDITOR
        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            AddressableAssetGroup syncGroup = settings.CreateGroup("SyncAddressables", false, false, true,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            syncGroup.GetSchema<BundledAssetGroupSchema>().BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;

            //Create prefab
            string guid = CreatePrefab(tempAssetFolder + "/synctest.prefab");
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, syncGroup);
            entry.address = m_PrefabKey;

            //Create Scenes
            string sceneGuid = CreateScene($"{tempAssetFolder}/SyncTestScene.unity");
            AddressableAssetEntry sceneEntry = settings.CreateOrMoveEntry(sceneGuid, syncGroup);
            sceneEntry.address = m_SceneKey;
        }

#endif

        protected void ReleaseOp(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
                handle.Release();
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void SyncAddressableLoad_CompletesWithValidKey()
        {
            var loadOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void SyncAddressableLoadAssets_CompletesWithValidKey()
        {
            var loadOp = m_Addressables.LoadAssetsAsync<GameObject>(new List<string>() { m_PrefabKey, m_SceneKey }, null, Addressables.MergeMode.Union, false);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void SyncAddressableLoadAssets_CompletesWithInvalidKey()
        {
            var loadOp = m_Addressables.LoadAssetsAsync<GameObject>(new List<string>() { m_PrefabKey, m_SceneKey, "bad key" }, null, Addressables.MergeMode.Union, false);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void SyncAddressableLoad_CompletesWithInvalidKey()
        {
            var loadOp = m_Addressables.LoadAssetAsync<GameObject>(m_InvalidKey);
            Assert.IsNull(loadOp.WaitForCompletion());
            LogAssert.Expect(LogType.Error, new Regex("InvalidKeyException*"));
            Assert.AreEqual(AsyncOperationStatus.Failed, loadOp.Status);
            Assert.IsNull(loadOp.Result);
            //Cleanup
            ReleaseOp(loadOp);
        }
        
        [Test]
        public void CheckForCatalogUpdates_CompletesSynchronously()
        {
            var checkForUpdates = m_Addressables.CheckForCatalogUpdates(false);
            Assert.IsNotNull(checkForUpdates.WaitForCompletion());
            Assert.AreEqual(AsyncOperationStatus.Succeeded, checkForUpdates.Status);
            Assert.IsTrue(checkForUpdates.IsDone);

            //Cleanup
            ReleaseOp(checkForUpdates);
        }

        [Test]
        public void CheckForCatalogUpdates_CompletesSynchronously_WhenAutoReleaseHandle()
        {
            var checkForUpdates = m_Addressables.CheckForCatalogUpdates();
            checkForUpdates.WaitForCompletion();
            Assert.IsFalse(checkForUpdates.IsValid());
            Assert.IsTrue(checkForUpdates.IsDone);
        }

        [Test]
        public void UpdateCatalogs_CompletesSynchronously()
        {
            var updateCatalogs = m_Addressables.UpdateCatalogs(null, false);
            Assert.IsNull(updateCatalogs.WaitForCompletion());
            LogAssert.Expect(LogType.Error, new Regex("Content update not available*"));
            LogAssert.Expect(LogType.Error, new Regex(".*ChainOperation.*"));
            Assert.AreEqual(AsyncOperationStatus.Failed, updateCatalogs.Status);
            Assert.IsTrue(updateCatalogs.IsDone);

            //Cleanup
            ReleaseOp(updateCatalogs);
        }

        [Test]
        public void GetDownloadSizeAsync_CompletesSynchronously()
        {
            var getDownloadSize = m_Addressables.GetDownloadSizeAsync((object)m_PrefabKey);
            Assert.IsNotNull(getDownloadSize.WaitForCompletion());
            Assert.AreEqual(AsyncOperationStatus.Succeeded, getDownloadSize.Status);
            Assert.IsTrue(getDownloadSize.IsDone);

            //Cleanup
            ReleaseOp(getDownloadSize);
        }

        [Test]
        public void DownloadDependencies_CompletesSynchronously()
        {
            var downloadDependencies = m_Addressables.DownloadDependenciesAsync((object)m_PrefabKey);
            var result = downloadDependencies.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, downloadDependencies.Status);
            Assert.IsNotNull(result);
            Assert.IsTrue(downloadDependencies.IsDone);

            //Cleanup
            ReleaseOp(downloadDependencies);
        }

        [Test]
        public void DownloadDependencies_CompletesSynchronously_WhenAutoReleased()
        {
            var downloadDependencies = m_Addressables.DownloadDependenciesAsync((object)m_PrefabKey, true);
            downloadDependencies.WaitForCompletion();
            Assert.IsTrue(downloadDependencies.IsDone);
        }

        [Test]
        public void ClearDependencyCache_CompletesSynchronously()
        {
            var clearCache = m_Addressables.ClearDependencyCacheAsync((object)m_PrefabKey, false);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, clearCache.Status);
            Assert.IsTrue(clearCache.WaitForCompletion());
            Assert.IsTrue(clearCache.IsDone);

            //Cleanup
            ReleaseOp(clearCache);
        }

        [Test]
        public void InstantiateSync_CompletesSuccessfully_WithValidKey()
        {
            var loadOp = m_Addressables.InstantiateAsync(m_PrefabKey);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.IsNotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void InstantiateMultipleObjectsSync_CompletesSuccessfully_WithValidKey()
        {
            var loadOp1 = m_Addressables.InstantiateAsync(m_PrefabKey);
            var result1 = loadOp1.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp1.Status);
            Assert.IsNotNull(result1);
            Assert.IsNotNull(loadOp1.Result);
            Assert.AreEqual(loadOp1.Result, result1);

            var loadOp2 = m_Addressables.InstantiateAsync(m_PrefabKey);
            var result2 = loadOp2.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp2.Status);
            Assert.IsNotNull(result2);
            Assert.IsNotNull(loadOp2.Result);
            Assert.AreEqual(loadOp2.Result, result2);

            Assert.AreNotEqual(result1, result2);

            //Cleanup
            ReleaseOp(loadOp1);
            ReleaseOp(loadOp2);
        }

        [Test]
        public void InstantiateSync_CompletesSuccessfully_WithInvalidKey()
        {
            var loadOp = m_Addressables.InstantiateAsync(m_InvalidKey);
            LogAssert.Expect(LogType.Error, new Regex("InvalidKeyException*"));
            Assert.AreEqual(AsyncOperationStatus.Failed, loadOp.Status);
            Assert.IsNull(loadOp.WaitForCompletion());
            Assert.IsNull(loadOp.Result);

            //Cleanup
            ReleaseOp(loadOp);
        }
        
        [Test]
        public void RequestingResourceLocation_CompletesSynchronously()
        {
            var requestOp = m_Addressables.LoadResourceLocationsAsync(m_PrefabKey);
            var result = requestOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, requestOp.Status);
            Assert.IsNotNull(result);
            Assert.IsNotNull(requestOp.Result);
            Assert.AreEqual(requestOp.Result, result);

            //Cleanup
            ReleaseOp(requestOp);
        }

        [Test]
        public void RequestingResourceLocations_CompletesSynchronously()
        {
            var requestOp = m_Addressables.LoadResourceLocationsAsync(new List<string>() { m_PrefabKey, m_SceneKey }, Addressables.MergeMode.Union);
            var result = requestOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, requestOp.Status);
            Assert.IsNotNull(result);
            Assert.IsNotNull(requestOp.Result);
            Assert.AreEqual(requestOp.Result, result);

            //Cleanup
            ReleaseOp(requestOp);
        }

        [Test]
        public void LoadContentCatalogSynchronously_SuccessfullyCompletes_WithValidPath()
        {
            string catalogPath = m_RuntimeSettingsPath.Replace("settings", "catalog");

            //There's no catalog created for fast mode.  Creating one at this point had issues on CI
            if (catalogPath.StartsWith("GUID:"))
                Assert.Ignore();

            var loadCatalogOp = m_Addressables.LoadContentCatalogAsync(catalogPath, false);
            var result = loadCatalogOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadCatalogOp.Status);
            Assert.IsNotNull(loadCatalogOp.Result);
            Assert.AreEqual(loadCatalogOp.Result, result);

            //Cleanup
            ReleaseOp(loadCatalogOp);
        }

        [Test]
        public void LoadContentCatalogSynchronously_SuccessfullyCompletes_WithInvalidPath()
        {
            LogAssert.Expect(LogType.Error, new Regex("Invalid path in TextDataProvider*"));
            LogAssert.Expect(LogType.Error, new Regex("Unable to load ContentCatalogData*"));
            LogAssert.Expect(LogType.Error, new Regex("Failed to load content catalog*"));
            LogAssert.Expect(LogType.Error, new Regex(".*ChainOperation.*"));

            var loadCatalogOp = m_Addressables.LoadContentCatalogAsync("not a real path.json", false);

            var result = loadCatalogOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Failed, loadCatalogOp.Status);
            Assert.IsNull(result);
            Assert.IsNull(loadCatalogOp.Result);

            //Cleanup
            ReleaseOp(loadCatalogOp);
        }

        [Test]
        public void InstanceOperation_WithFailedBundleLoad_CompletesSync()
        {
            var depOp = m_Addressables.LoadAssetAsync<GameObject>(m_InvalidKey);
            LogAssert.Expect(LogType.Error, new Regex("InvalidKeyException*"));
            
            var instanceOperation = new ResourceManager.InstanceOperation();
            instanceOperation.Init(m_Addressables.ResourceManager, new InstanceProvider(), new InstantiationParameters(), depOp);
            //Since we're calling the operation directly we need to simulate the full workflow of the additional ref count during load
            instanceOperation.IncrementReferenceCount();

            instanceOperation.WaitForCompletion();
            LogAssert.Expect(LogType.Error, new Regex("InvalidKeyException*"));

            Assert.IsTrue(instanceOperation.IsDone);
            m_Addressables.Release(depOp);
        }
        
        [Test]
        public void InstantiateSync_InvalidKeyExceptionCorrectlyThrown()
        {
            var prevHandler = ResourceManager.ExceptionHandler;
            ResourceManager.ExceptionHandler = (handle, exception) =>
            {
                Assert.AreEqual(typeof(InvalidKeyException), exception.GetType(),
                    "Exception thrown is not of the correct type.");
            };
            var depOp = m_Addressables.LoadAssetAsync<GameObject>(m_InvalidKey);
            m_Addressables.Release(depOp);
            ResourceManager.ExceptionHandler = prevHandler;
        }

        [Test]
        public void InstanceOperation_WithSuccessfulBundleLoad_CompletesSync()
        {
                var depOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey);
                var instanceOperation = new ResourceManager.InstanceOperation();
                instanceOperation.Init(m_Addressables.ResourceManager, new InstanceProvider(), new InstantiationParameters(), depOp);
                //Since we're calling the operation directly we need to simulate the full workflow of the additional ref count during load
                instanceOperation.IncrementReferenceCount();

                instanceOperation.WaitForCompletion();

                Assert.IsTrue(instanceOperation.IsDone);
                m_Addressables.Release(depOp);
        }

        class FailedAssetBundleResource : IAssetBundleResource
        {
            public AssetBundle GetAssetBundle()
            {
                return null;
            }
        }
    }

    //This class is made because of, and should be refactored away when resolved, bug: https://jira.unity3d.com/browse/ADDR-1215
    public abstract class SyncAddressablesWithSceneTests : SyncAddressableTests
    {
        [Test]
        public void LoadingScene_CompletesSynchronously()
        {
            var loadOp = m_Addressables.LoadSceneAsync(m_SceneKey, LoadSceneMode.Additive);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.IsNotNull(result);
            Assert.IsTrue(loadOp.IsDone);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            m_Addressables.UnloadSceneAsync(loadOp).WaitForCompletion();
            ReleaseOp(loadOp);
        }

        [Test]
        public void UnloadingScene_CompletesSynchronously_WhenAutoReleasingHandle()
        {
            var loadOp = m_Addressables.LoadSceneAsync(m_SceneKey, LoadSceneMode.Additive);
            loadOp.WaitForCompletion();

            var unloadOp = m_Addressables.UnloadSceneAsync(loadOp);
            unloadOp.WaitForCompletion();
            Assert.IsTrue(unloadOp.IsDone);
        }

        [Test]
        public void UnloadingScene_CompletesSynchronously()
        {
            var loadOp = m_Addressables.LoadSceneAsync(m_SceneKey, LoadSceneMode.Additive);
            loadOp.WaitForCompletion();

            var unloadOp = m_Addressables.UnloadSceneAsync(loadOp, false);
            var result = unloadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, unloadOp.Status);
            Assert.IsTrue(unloadOp.IsDone);
            Assert.AreEqual(unloadOp.Result, result);

            //Cleanup
            ReleaseOp(unloadOp);
        }
    }

#if UNITY_EDITOR
    class SyncAddressableTests_FastMode : SyncAddressablesWithSceneTests { protected override TestBuildScriptMode BuildScriptMode { get { return TestBuildScriptMode.Fast; } } }

    class SyncAddressableTests_VirtualMode : SyncAddressablesWithSceneTests { protected override TestBuildScriptMode BuildScriptMode { get { return TestBuildScriptMode.Virtual; } } }

    class SyncAddressableTests_PackedPlaymodeMode : SyncAddressablesWithSceneTests { protected override TestBuildScriptMode BuildScriptMode { get { return TestBuildScriptMode.PackedPlaymode; } } }
#endif

    [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
    class SyncAddressableTests_PackedMode : SyncAddressableTests { protected override TestBuildScriptMode BuildScriptMode { get { return TestBuildScriptMode.Packed; } } }
}
