﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;


#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Febucci.HierarchyData
{

    // Copyright (c) 2020 Federico Bellucci - febucci.com
    // 
    // Permission is hereby granted, free of charge, to any person obtaining a copy of this software/algorithm and associated
    // documentation files (the "Software"), to use, copy, modify, merge or distribute copies of the Software, and to permit
    // persons to whom the Software is furnished to do so, subject to the following conditions:
    // 
    // - The Software, substantial portions, or any modified version be kept free of charge and cannot be sold commercially.
    // 
    // - The above copyright and this permission notice shall be included in all copies, substantial portions or modified
    // versions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
    // WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
    // COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
    // OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    // 
    // For any other use, please ask for permission by contacting the author.
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class HierarchyDrawer
    {
#if UNITY_EDITOR
        static HierarchyDrawer()
        {
            //Initialize();
            EditorApplication.delayCall -= HierarchyDrawer.Initialize;
            EditorApplication.delayCall += HierarchyDrawer.Initialize;
        }

        

        static class HierarchyRenderer
        {
            static private HierarchyDataProfile.TreeData.BranchGroup currentBranch;

            private static readonly HierarchyDataProfile.TreeData.BranchGroup fallbackGroup =
                new HierarchyDataProfile.TreeData.BranchGroup()
                {
                    overlayColor = new Color(1f, 0.44f, 0.97f, .04f),
                    colors = new[]
                    {
                        new Color(1f, 0.44f, 0.97f), new Color(0.56f, 0.44f, 1f), new Color(0.44f, 0.71f, 1f),
                        new Color(0.19f, 0.53f, 0.78f)
                    }
                };
            
            public static void SwitchBranchesColors(int hierarchyIndex)
            {
                int targetIndex = hierarchyIndex % data.Tree.branches.Length;
                if (data.Tree.branches.Length == 0 || data.Tree.branches[targetIndex].colors.Length<=0)
                {
                    currentBranch = fallbackGroup;
                    return;
                }
                
                currentBranch = data.Tree.branches[targetIndex];
            }
            
            private const float barWidth = 2;
            
            public static void DrawNestGroupOverlay(Rect originalRect)
            {
                if (currentBranch.overlayColor.a <= 0) return;
                
                originalRect = new Rect(32, originalRect.y, originalRect.width + (originalRect.x-32), originalRect.height);
                EditorGUI.DrawRect(originalRect, currentBranch.overlayColor);
            }

            static float GetStartX(Rect originalRect, int nestLevel)
            {
                return 37 + (originalRect.height-2) * nestLevel;
                //return originalRect.x                //aligned start position (9 is the magic number here)
                //    - originalRect.height * 2 //GameObject icon offset
                //    + 9
                //    - nestLevel * (originalRect.height - 2);       
            }

            static Color GetNestColor(int nestLevel)
            {
                return currentBranch.colors[nestLevel % currentBranch.colors.Length];
            }

            public static void DrawVerticalLineFrom(Rect originalRect, int nestLevel)
            {
                DrawHalfVerticalLineFrom(originalRect, true, nestLevel);   
                DrawHalfVerticalLineFrom(originalRect, false, nestLevel);   
            }

            public static void DrawHalfVerticalLineFrom(Rect originalRect, bool startsOnTop, int nestLevel)
            {
                if(currentBranch.colors.Length<=0) return;

                DrawHalfVerticalLineFrom(originalRect, startsOnTop, nestLevel, GetNestColor(nestLevel));
            }
            
            public static void DrawHalfVerticalLineFrom(Rect originalRect, bool startsOnTop, int nestLevel, Color color)
            {
                //Vertical rect, starts from the very left and then proceeds to te right
                EditorGUI.DrawRect(
                    new Rect(
                        GetStartX(originalRect, nestLevel), 
                        startsOnTop ? originalRect.y : (originalRect.y + originalRect.height/2f), 
                        barWidth, 
                        originalRect.height/2f
                        ), 
                    color
                    );
            }

            public static void DrawHorizontalLineFrom(Rect originalRect, int nestLevel, bool hasChilds)
            {
                if(currentBranch.colors.Length<=0) return;
                
                //Vertical rect, starts from the very left and then proceeds to te right
                EditorGUI.DrawRect(
                    new Rect(
                        GetStartX(originalRect, nestLevel), 
                        originalRect.y  + originalRect.height/2f, 
                        originalRect.height + (hasChilds ? -5 :  2), 
                        //originalRect.height - 5, 
                        barWidth
                        ), 
                    GetNestColor(nestLevel)
                    );
            }
        }
        
        #region Types

        [Serializable]
        struct InstanceInfo
        {
            /// <summary>
            /// Contains the indexes for each icon to draw, eg. draw(iconTextures[iconIndexes[0]]);
            /// </summary>
            public List<int> iconIndexes;
            public bool isSeparator;
            public string goName;
            public int prefabInstanceID;
            public bool isGoActive;

            public bool isLastElement;
            public bool hasChilds;
            public bool topParentHasChild;
            
            public int nestingGroup;
            public int nestingLevel;
        }

        #endregion

        private static bool initialized = false;
        private static IHierarchyData data;
        private static int firstInstanceID = 0;
        private static List<int> iconsPositions = new List<int>();
        private static Dictionary<int, InstanceInfo> sceneGameObjects = new Dictionary<int, InstanceInfo>();
        private static Dictionary<int, Color> prefabColors = new Dictionary<int, Color>();

        #region Menu Items

        #region Internal

        [MenuItem("Tools/Custom Hierarchy/Wake Up", priority = 1)]
        public static void WakeUpHierarchyDrawer()
        {
            HierarchyDrawer.Initialize();
        }

        #endregion

        #region Blog

        [MenuItem("Tools/Febucci/📝 Blog", priority = 50)]
        static void m_OpenBlog()
        {
            Application.OpenURL("https://www.febucci.com/blog");
        }

        [MenuItem("Tools/Febucci/🐦 Twitter", priority = 51)]
        static void m_OpenTwitter()
        {
            Application.OpenURL("https://twitter.com/febucci");
        }

        #endregion

        #endregion

        #region Initialization/Helpers

        static IHierarchyData LoadHierarchyData()
        {
            IHasHierarchyData globalTuner;
            
            var asyncHandle = Addressables.LoadAssetAsync<IHasHierarchyData>("GlobalTuner.asset");
            //Debug.Log(asyncHandle.Status);


            if (asyncHandle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogWarning("Could not load GlobalTuner.");
                return null;
            }

            globalTuner = asyncHandle.WaitForCompletion();
            if (globalTuner.HierarchyData == null)
            {
                Debug.LogWarning("Could not load HierarchyData.");
                return null;
            }
            
            IHierarchyData hierarchyData = globalTuner.HierarchyData;

            return hierarchyData;
        }


        public static void Initialize()
        {
            #region Unregisters previous events

            if (initialized)
            {
                //Prevents registering events multiple times
                EditorApplication.hierarchyWindowItemOnGUI -= DrawCore;
                EditorApplication.hierarchyChanged -= RetrieveDataFromScene;
            }

            #endregion

            initialized = false;
            data = LoadHierarchyData();

            if (data == null) return; //no data found

            initialized = true;

            if (data.HasProfile && data.Enabled)
            {

                #region Registers events

                EditorApplication.hierarchyWindowItemOnGUI += DrawCore;
                EditorApplication.hierarchyChanged += RetrieveDataFromScene;

                #endregion

                RetrieveDataFromScene();
                
                prefabColors.Clear();
                foreach (var prefab in data.PrefabData.prefabs)
                {
                    if (prefab.color.a<=0) continue;
                    if (!prefab.gameObject) continue;
                    
                    int instanceID = prefab.gameObject.GetInstanceID();
                    if(prefabColors.ContainsKey(instanceID)) continue;
                    
                    prefabColors.Add(instanceID, prefab.color);
                }
            }

            EditorApplication.RepaintHierarchyWindow();
        }

        #endregion

        /// <summary>
        /// Updates the list of objects to draw, icons etc.
        /// </summary>
        static void RetrieveDataFromScene()
        {
            if (!data.UpdateInPlayMode && Application.isPlaying) //temp. fix for performance reasons while in play mode
                return;

            sceneGameObjects.Clear();
            iconsPositions.Clear();

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                var prefabContentsRoot = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                
                AnalyzeGoWithChildren(
                    go: prefabContentsRoot,
                    nestingLevel: -1,
                    prefabContentsRoot.transform.childCount > 0,
                    nestingGroup: 0,
                    isLastChild: true);

                firstInstanceID = prefabContentsRoot.GetInstanceID();

                return;
            }

            GameObject[] sceneRoots;
            Scene tempScene;
            firstInstanceID = -1;
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                tempScene = SceneManager.GetSceneAt(i);
                if (tempScene.isLoaded)
                {
                    sceneRoots = tempScene.GetRootGameObjects();
                    //Analyzes all scene's gameObjects
                    for (int j = 0; j < sceneRoots.Length; j++)
                    {
                        AnalyzeGoWithChildren(
                            go: sceneRoots[j],
                            nestingLevel: 0,
                            sceneRoots[j].transform.childCount > 0,
                            nestingGroup: j,
                            isLastChild: j == sceneRoots.Length - 1
                            );
                    }

                    if (firstInstanceID == -1 && sceneRoots.Length > 0) firstInstanceID = sceneRoots[0].GetInstanceID();
                }
            }
        }

        static void AnalyzeGoWithChildren(GameObject go, int nestingLevel, bool topParentHasChild, int nestingGroup, bool isLastChild)
        {
            int instanceID = go.GetInstanceID();

            if (!sceneGameObjects.ContainsKey(instanceID)) //processes the gameobject only if it wasn't processed already
            {
                InstanceInfo newInfo = new InstanceInfo();
                newInfo.iconIndexes = new List<int>();
                newInfo.isLastElement = isLastChild && go.transform.childCount == 0;
                newInfo.nestingLevel = nestingLevel;
                newInfo.nestingGroup = nestingGroup;
                newInfo.hasChilds = go.transform.childCount > 0;
                newInfo.isGoActive = go.activeInHierarchy;
                newInfo.topParentHasChild = topParentHasChild;
                newInfo.goName = go.name;

                if (data.PrefabData.enabled)
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (prefab)
                    {
                        newInfo.prefabInstanceID = prefab.GetInstanceID();
                    }
                }

                newInfo.isSeparator = String.Compare(go.tag, "EditorOnly", StringComparison.Ordinal) == 0 //gameobject has EditorOnly tag
                                      && (!string.IsNullOrEmpty(go.name) && !string.IsNullOrEmpty(data.Separator.startString) && go.name.StartsWith(data.Separator.startString)); //and also starts with '>'

                if (data.Icons.enabled && data.Icons.pairs!=null && data.Icons.pairs.Length>0)
                {

                    #region Components Information (icons)

                    Type classReferenceType; //todo opt
                    Type componentType;

                    foreach (var c in go.GetComponents<Component>())
                    {
                        if(!c) continue;

                        componentType = c.GetType();

                        for (int elementIndex = 0; elementIndex < data.Icons.pairs.Length; elementIndex++)
                        {
                            if (!data.Icons.pairs[elementIndex].iconToDraw) continue;

                            //Class inherithance
                            foreach (var classReference in data.Icons.pairs[elementIndex].targetClasses)
                            {
                                if(!classReference) continue;
                                
                                classReferenceType = classReference.GetClass();
                                
                                if(!classReferenceType.IsClass) continue;

                                //class ineriths 
                                if (componentType.IsAssignableFrom(classReferenceType) || componentType.IsSubclassOf(classReferenceType))
                                {
                                    //Adds the icon index to the "positions" list, to draw all of them in order later [if enabled] 
                                    if (!iconsPositions.Contains(elementIndex)) iconsPositions.Add(elementIndex);

                                    //Adds the icon index to draw, only if it's not present already
                                    if (!newInfo.iconIndexes.Contains(elementIndex))
                                        newInfo.iconIndexes.Add(elementIndex);


                                    break;
                                }
                            }
                        }
                    }

                    newInfo.iconIndexes.Sort();

                    #endregion
                }

                //Adds element to the array
                sceneGameObjects.Add(instanceID, newInfo);
            }

            #region Analyzes Childrens

            int childCount = go.transform.childCount;
            for (int j = 0; j < childCount; j++)
            {
                AnalyzeGoWithChildren(
                    go.transform.GetChild(j).gameObject,
                    nestingLevel + 1,
                    topParentHasChild,
                    nestingGroup,
                    j == childCount - 1
                    );
            }

            #endregion
        }

        #region Drawing

        private static bool temp_alternatingDrawed;
        private static int temp_iconsDrawedCount;
        private static InstanceInfo currentItem;
        private static bool drawedPrefabOverlay;

        
        static void DrawCore(int instanceID, Rect selectionRect)
        {
            //skips early if item is not registered or not valid
            if (!sceneGameObjects.ContainsKey(instanceID)) return;

            currentItem = sceneGameObjects[instanceID];
            temp_iconsDrawedCount = -1;
            GameObject go = null;

            if (instanceID == firstInstanceID)
            {
                temp_alternatingDrawed = currentItem.nestingGroup %2 == 0;
            }

            #region Draw Activation Toggle
            if (data.DrawActivationToggle)
            {
                temp_iconsDrawedCount++;

                go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

                if (go == null)
                    return;

                var r = new Rect(selectionRect.xMax - 16 * (temp_iconsDrawedCount + 1) - 2, selectionRect.yMin, 16, 16);

                var wasActive = go.activeSelf;
                var isActive = GUI.Toggle(r, wasActive, "");
                if (wasActive != isActive)
                {
                    go.SetActive(isActive);
                    if (EditorApplication.isPlaying == false)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                        EditorUtility.SetDirty(go);
                    }
                }
            }
            #endregion

            #region Draw Alternating BG

            if (data.AlternatingBackground.enabled)
            {
                if (temp_alternatingDrawed)
                {
                    EditorGUI.DrawRect(selectionRect, data.AlternatingBackground.color);
                    temp_alternatingDrawed = false;
                }
                else
                {
                    temp_alternatingDrawed = true;
                }
            }
            

            #endregion

            #region DrawingPrefabsBackground

            drawedPrefabOverlay = false;
            if (data.PrefabData.enabled && prefabColors.Count>0)
            {
                if (prefabColors.ContainsKey(currentItem.prefabInstanceID))
                {
                    EditorGUI.DrawRect(selectionRect, prefabColors[currentItem.prefabInstanceID]);
                    drawedPrefabOverlay = true;
                }
            }
            

            #endregion
            
            #region Drawing Tree

            if (data.Tree.enabled
                && currentItem.nestingLevel >= 0)
            {
                if (selectionRect.x >= 60) //prevents drawing when the hierarchy search mode is enabled 
                {
                    HierarchyRenderer.SwitchBranchesColors(currentItem.nestingGroup);

                    //Group
                    if ((data.Tree.drawOverlayOnColoredPrefabs || !drawedPrefabOverlay) && currentItem.topParentHasChild)
                    {
                        HierarchyRenderer.DrawNestGroupOverlay(selectionRect);
                    }
                    

                    if (currentItem.nestingLevel == 0 && !currentItem.hasChilds)
                    {
                        HierarchyRenderer.DrawHalfVerticalLineFrom(selectionRect, true, 0, data.Tree.baseLevelColor);
                        HierarchyRenderer.DrawHalfVerticalLineFrom(selectionRect, false, 0, data.Tree.baseLevelColor);
                    }
                    else
                    {
                        //Draws a vertical line for each previous nesting level
                        for (int i = 0; i <= currentItem.nestingLevel; i++)
                        {
                            HierarchyRenderer.DrawVerticalLineFrom(selectionRect, i);
                        }
                        
                            HierarchyRenderer.DrawHorizontalLineFrom(
                                selectionRect, currentItem.nestingLevel, currentItem.hasChilds
                                );
                        
                    }

                }

                //draws a super small divider between different groups
                if (currentItem.nestingLevel == 0 && data.Tree.dividerHeigth > 0)
                {
                    Rect boldGroupRect = new Rect(
                        32, selectionRect.y - data.Tree.dividerHeigth / 2f,
                        selectionRect.width + (selectionRect.x - 32),
                        data.Tree.dividerHeigth
                        );
                    EditorGUI.DrawRect(boldGroupRect, Color.black * .3f);
                }
                

            }

            #endregion

            #region Drawing Separators
            
            //EditorOnly objects are only removed from build if they're not childrens
            if (data.Separator.enabled && data.Separator.color.a >0
                                       && currentItem.isSeparator && currentItem.nestingLevel == 0)
            {
                //Adds color on top of the label
                EditorGUI.DrawRect(selectionRect, data.Separator.color);
            }

            #endregion

            #region Drawing Icon

            if (data.Icons.enabled)
            {
                #region Local Method

                //Draws each component icon
                void DrawIcon(int textureIndex)
                {
                    //---Icon Alignment---
                    if (data.Icons.aligned)
                    {
                        //Aligns icon based on texture's position on the array
                        int CalculateIconPosition()
                        {
                            for (int i = 0; i < iconsPositions.Count; i++)
                            {
                                if (iconsPositions[i] == textureIndex) return i;
                            }

                            return 0;
                        }

                        temp_iconsDrawedCount = CalculateIconPosition();
                    }
                    else
                    {
                        temp_iconsDrawedCount++;
                    }
                    //---

                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * (temp_iconsDrawedCount + 1) - 2, selectionRect.yMin, 16, 16),
                        data.Icons.pairs[textureIndex].iconToDraw
                        );
                }

                #endregion

                {
                    //Draws the gameobject icon, if present
                    var content = EditorGUIUtility.ObjectContent(go ?? EditorUtility.InstanceIDToObject(instanceID), null);
                    
                    if (content.image && !string.IsNullOrEmpty(content.image.name))
                    {
                        if (content.image.name != "d_GameObject Icon" && content.image.name != "d_Prefab Icon")
                        {
                            temp_iconsDrawedCount++;
                            GUI.DrawTexture(
                                new Rect(
                                    selectionRect.xMax - 16 * (temp_iconsDrawedCount + 1) - 2, selectionRect.yMin, 16,
                                    16
                                    ),
                                content.image
                                );
                        }
                    }
                }
                
                
                
                for (int i = 0; i < currentItem.iconIndexes.Count; i++)
                {
                    DrawIcon(currentItem.iconIndexes[i]);
                }
            }

            #endregion
        }

        #endregion
#endif
    }

}
