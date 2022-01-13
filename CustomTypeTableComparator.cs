
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
#if ifland_avatar_3_0
using ifland.AvatarEngine;
#else
using Treal.AvatarFramework;
#endif
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

public class CustomTypeTableComparator : MonoBehaviour
{
    [Header("Auto Copy Property")]
    [Tooltip("이전 버전 테이블")]
    public CustomizeTypeTable oldTable;
    [Tooltip("최신 아이템이 반영된 테이블")]
    public CustomizeTypeTable newTable;
    private List<string> newCategoryList = new List<string>();
    private List<string> deletedCategoryList = new List<string>();
    private List<string> newItemList = new List<string>();
    private List<string> deletedItemList = new List<string>();
    private List<string> newPartsList = new List<string>();
    private List<string> deletedPartsList = new List<string>();
    private List<string> newMaterialList = new List<string>();
    private List<string> deletedMaterialList = new List<string>();
    private float timer;

    [Header("Compare Item Property")]
    public CustomizeTypeTable target1;
    public CustomizeTypeTable target2;
    public CategoryComparatorItem result = new CategoryComparatorItem();

    [Header("Json To Csv Property")]
    public string jsonPath;
    public TargetType targetType;
    public enum TargetType
    {
        NEWITEMLIST,
        DELETEDITEMLIST
    };

    [Header("Combine Table Property")]
    public CustomizeTypeTable baseTable;
    public CustomizeTypeTable addedTable;

    private List<string> addedCategoryList = new List<string>();
    private List<string> addedItemList = new List<string>();
    private List<string> overlapItemList = new List<string>();
    private List<string> addedPartsList = new List<string>();
    private List<string> overlapPartsList = new List<string>();

    [Header("PrintActionListProperty")]
    public AvatarCore avatarCore;

    [ContextMenu("Print Action List")]
    private void PrintActionListProperty()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Avatar Action Name\n\n");

        var actionList = avatarCore.AvatarActionList;
        foreach (var action in actionList)
        {
            sb.Append(action.ActionName).Append('\n');
        }

        string dstDir = Application.dataPath + $"/../ReleaseList{System.DateTime.Now.Month:00}{System.DateTime.Now.Day:00}/";
        if (!System.IO.Directory.Exists(dstDir)) System.IO.Directory.CreateDirectory(dstDir);
        string dst = dstDir + $"ActionList_{System.DateTime.Now.Month:00}{System.DateTime.Now.Day:00}.txt";
        if (System.IO.File.Exists(dst)) System.IO.File.Delete(dst);
        System.IO.File.WriteAllText(dst, sb.ToString());
        Debug.Log("Action List가 아래 경로에 저장되었습니다.\n" + dstDir);
    }

    [ContextMenu("Auto Copy")]
    private void AutoCopy()
    {
        if (!EditorUtility.DisplayDialog("주의사항", "이 기능은 이전버전 테이블을\n직접적으로 수정하며, 복구기능은 없습니다.\n계속 실행합니까?", "실행", "취소"))
        {
            return;
        }
        timer = Time.realtimeSinceStartup;
        CopyCategoryAndItemTable();
        CopyPartsTable();
        FinishProcess();
    }

    [ContextMenu("CompareItem")]
    public void CompareTwoTypeTable()
    {
        bool target1null = target1 == null;
        bool target2null = target2 == null;
        if (target1null || target2null)
        {
            Debug.Log($"한개 이상의 테이블이 비어있습니다. MonoBehaviour에 테이블을 세팅해주세요.\n<color=blue>빈 테이블 : {(target1 == null ? "Target1, " : "")}{(target2 == null ? "Target2 " : "")}</color>");
            return;
        }
        float time = Time.realtimeSinceStartup;
        var tmp = new CategoryComparatorItem();
        for (var i = 0; i < target1.categoryList.Count; i++)
        {
            var target1Category = target1.categoryList[i];
            var target2Category = target2.categoryList.Find((x) => x.categoryKey == target1Category.categoryKey);
            if (target2Category == null)
            {
                CategoryItemList delItemList = new CategoryItemList();

                tmp.delCaterogyList.Add(target1Category.categoryKey);
                tmp.delItemList.Add(delItemList);
                delItemList.categoryName = target1Category.categoryKey;

                for (var j = 0; j < target1Category.itemList.Count; j++)
                {
                    delItemList.itemNameList.Add(target1Category.itemList[j].itemKey);
                }
            }
            else
            {
                CategoryItemList newItemList = new CategoryItemList();
                CategoryItemList delItemList = new CategoryItemList();

                newItemList.categoryName = target1Category.categoryKey;
                delItemList.categoryName = target1Category.categoryKey;

                for (var j = 0; j < target2Category.itemList.Count; j++)
                {
                    var idx = target1Category.itemList.FindIndex((x) => x.itemKey == target2Category.itemList[j].itemKey);
                    if (idx < 0)
                    {
                        if (target2Category.itemList[j].thumbnail != null)
                        {
                            CopyThumbnailFile(target2Category.itemList[j].thumbnail, target2Category.itemList[j].itemKey);
                        }
                        newItemList.itemNameList.Add(target2Category.itemList[j].itemKey);
                    }
                }

                for (var j = 0; j < target1Category.itemList.Count; j++)
                {
                    var idx = target2Category.itemList.FindIndex((x) => x.itemKey == target1Category.itemList[j].itemKey);
                    if (idx < 0) delItemList.itemNameList.Add(target1Category.itemList[j].itemKey);
                }

                if (newItemList.itemNameList.Count > 0)
                {
                    tmp.newItemList.Add(newItemList);
                }
                if (delItemList.itemNameList.Count > 0)
                {
                    tmp.delItemList.Add(delItemList);
                }
            }
        }

        for (var i = 0; i < target2.categoryList.Count; i++)
        {
            var target2Category = target2.categoryList[i];
            var target1Category = target1.categoryList.Find((x) => x.categoryKey == target2Category.categoryKey);

            if (target1Category == null)
            {

                CategoryItemList newItemList = new CategoryItemList();

                tmp.newCaterogyList.Add(target2Category.categoryKey);
                tmp.newItemList.Add(newItemList);
                newItemList.categoryName = target2Category.categoryKey;

                for (var j = 0; j < target2Category.itemList.Count; j++)
                {
                    newItemList.itemNameList.Add(target2Category.itemList[j].itemKey);
                    if (target2Category.itemList[j].thumbnail != null)
                    {
                        CopyThumbnailFile(target2Category.itemList[j].thumbnail, target2Category.itemList[j].itemKey);
                    }

                }
            }
        }
        result = tmp;
        WriteJsonFile(result);
        string dstDir = Application.dataPath + $"/../ReleaseList{System.DateTime.Now.Month:00}{System.DateTime.Now.Day:00}/";
        dstDir = dstDir.Replace("Assets/../", "");
        Debug.Log($"1번 테이블과 2번 테이블간 비교가 완료되었습니다.\n소요시간 : {Time.realtimeSinceStartup - time}");
        Debug.Log("신규 썸네일, Json List가 아래 경로에 복사되었습니다.");
        Debug.Log(dstDir);
    }

    [ContextMenu("Json To CSV")]
    public void JsonToCsv()
    {
        bool isNewItemList = targetType == TargetType.NEWITEMLIST;
        string csvName = isNewItemList ? "_new.csv" : "_deleted.csv";
        string csvPath = jsonPath.Replace(".json", csvName);
        string[] header = { "아이템 타입", "아바타 아이템", "색상 명칭", "아이템 기본 색상" };
        string strSeperator = ",";
        List<string[]> lineList = new List<string[]>();
        StringBuilder sb = new StringBuilder();
        CategoryComparatorItem cci = new CategoryComparatorItem();

        try
        {
            string text = File.ReadAllText(jsonPath);
            cci = JsonUtility.FromJson<CategoryComparatorItem>(text);
        }
        catch
        {
            Debug.Log("File Read / Json Parsing Error. Please Check jsonPath");
            return;
        }

        sb.AppendLine(string.Join(strSeperator, header));

        List<CategoryItemList> itemList = isNewItemList ? cci.newItemList : cci.delItemList;
        foreach (var item in itemList)
        {
            // TODO: 필요시 Color 에 대한 예외처리
            foreach (var name in item.itemNameList)
            {
                string[] line = { item.categoryName, name, "", "" };
                lineList.Add(line);
            }
        }

        // TODO: 필요시 Color 에 대한 예외처리

        foreach (var line in lineList)
        {
            sb.AppendLine(string.Join(strSeperator, line));
        }

        File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
        // File.AppendAllText(csvPath, sb.ToString());

        Debug.Log("Json to CSV Done: " + csvPath);
    }

    [ContextMenu("Json To CSV Test")]
    public void JsonToCsv_Test()
    {
        bool isNewItemList = targetType == TargetType.NEWITEMLIST;
        string csvName = isNewItemList ? "_new.csv" : "_deleted.csv";
        string csvPath = jsonPath.Replace(".json", csvName);
        string[] header = { "아이템 타입", "아바타 아이템", "색상 명칭", "아이템 기본 색상", "Typename" };
        string strSeperator = ",";
        List<string[]> lineList = new List<string[]>();
        StringBuilder sb = new StringBuilder(); //https://hardlearner.tistory.com/288

        if (jsonPath.ToLower().Contains("animation"))    //ToLower() -> 소문자 컨버트, Contains() -> 해당 문자열 포함 확인(T/F)
        {
            Debug.Log("yet");
        }
        else
        {
            //Main, Parts, Deform은 해당 코드 반복
            CustomizeTypeTableDescript des = new CustomizeTypeTableDescript();

            try
            {
                string text = File.ReadAllText(jsonPath);
                des = JsonUtility.FromJson<CustomizeTypeTableDescript>(text);   //https://m.blog.naver.com/wolat/220865546178
            }
            catch
            {
                Debug.Log("File Read / Json Parsing Error. Please Check jsonPath");
                return;
            }

            sb.AppendLine(string.Join(strSeperator, header));


            // TODO: 필요시 Color 에 대한 예외처리
            foreach (var category in des.categoryList)
            {
                foreach (var item in category.itemList)
                {
                    string[] line = { category.categoryKey, item.itemKey, "", "", "" };
                    lineList.Add(line);
                }
            }


            // TODO: 필요시 Color 에 대한 예외처리

            foreach (var line in lineList)
            {
                sb.AppendLine(string.Join(strSeperator, line));
            }

            File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
            // File.AppendAllText(csvPath, sb.ToString());

            Debug.Log("Json to CSV Done: " + csvPath);
        }
    }

    // newTable로부터 oldTable로
    // 카테고리 및 아이템 테이블을 복사합니다
    private void CopyCategoryAndItemTable()
    {
        foreach (var oldCategory in oldTable.categoryList)
        {
            var category = newTable.FindCategoryTable(oldCategory.categoryKey);
            if (category == null)
            {
                // 삭제된 카테고리
                deletedCategoryList.Add(oldCategory.categoryKey);
                foreach (var oldItem in oldCategory.itemList)
                {
                    deletedItemList.Add(oldItem.itemKey);
                }
                continue;
            }

            foreach (var oldItem in oldCategory.itemList)
            {
                var item = category.FindItemTable(oldItem.itemKey);
                if (item == null)
                {
                    // 삭제된 아이템
                    deletedItemList.Add(oldItem.itemKey);
                }
            }
        }

        foreach (var deletedCategoryKey in deletedCategoryList)
        {
            oldTable.categoryList.Remove(oldTable.categoryList.Find(x => x.categoryKey.Equals(deletedCategoryKey)));
        }

        foreach (var category in newTable.categoryList)
        {
            var oldCategory = oldTable.FindCategoryTable(category.categoryKey);
            if (oldCategory == null)
            {
                // 신규 카테고리
                newCategoryList.Add(category.categoryKey);
                CustomizeTypeTable.CategoryTable newCategory = new CustomizeTypeTable.CategoryTable();
                newCategory.categoryKey = category.categoryKey;
                newCategory.name = category.name;
                foreach (var dependencyCategory in category.dependencyCategoryList)
                {
                    newCategory.dependencyCategoryList.Add(dependencyCategory);
                }

                oldCategory = newCategory;
                oldTable.categoryList.Add(oldCategory);
            }

            foreach (var item in category.itemList)
            {
                var oldItem = oldCategory.FindItemTable(item.itemKey);
                if (oldItem == null)
                {
                    // 신규 아이템
                    newItemList.Add(item.itemKey);
                }
            }

            oldCategory.itemList.Clear();
            foreach (var item in category.itemList)
            {
                oldCategory.itemList.Add(item);
            }
        }
    }

    // oldTable의 파츠 리스트를 갱신합니다
    private void CopyPartsTable()
    {
        foreach (var category in newTable.categoryList)
        {
            var oldCategory = oldTable.FindCategoryTable(category.categoryKey);
            foreach (var parts in category.partsList)
            {
                var oldParts = oldCategory.FindPartsTable(parts.partsKey);
                if (oldParts == null)
                {
                    // 신규 파츠
                    newPartsList.Add(parts.partsKey);
                    CustomizeTypeTable.PartsTable newParts = new CustomizeTypeTable.PartsTable();
                    newParts.partsKey = parts.partsKey;
                    newParts.prefab = parts.prefab;
                    oldParts = newParts;
                    oldCategory.partsList.Add(newParts);
                }

                if (parts.materialList != null && parts.materialList.Count > 0)
                {
                    if (oldParts.materialList == null)
                    {
                        oldParts.materialList = new List<CustomizeTypeTable.PartsTable.MaterialTable>();
                    }
                    foreach (var matTable in parts.materialList)
                    {
                        var oldMatTable = oldParts.FindMaterialTable(matTable.materialKey);
                        if (oldMatTable == null || oldMatTable.material == null || string.IsNullOrEmpty(oldMatTable.materialKey))
                        {
                            // 신규 머티리얼
                            newMaterialList.Add(matTable.materialKey);
                            var newMatTable = new CustomizeTypeTable.PartsTable.MaterialTable();
                            newMatTable.materialKey = matTable.materialKey;
                            newMatTable.material = matTable.material;
                            oldParts.materialList.Add(newMatTable);
                            continue;
                        }
                    }
                }
            }
        }

        foreach (var oldCategory in oldTable.categoryList)
        {
            var category = newTable.FindCategoryTable(oldCategory.categoryKey);
            foreach (var oldParts in oldCategory.partsList)
            {
                var parts = category.FindPartsTable(oldParts.partsKey);
                if (parts == null)
                {
                    // 삭제된 파츠
                    deletedPartsList.Add(oldParts.partsKey);
                    continue;
                }

                if (oldParts.materialList != null)
                {
                    foreach (var oldMaterial in oldParts.materialList)
                    {
                        var material = parts.FindMaterialTable(oldMaterial.materialKey);
                        if (material == null)
                        {
                            // 삭제된 머티리얼
                            deletedMaterialList.Add(oldMaterial.materialKey);
                        }
                    }

                    foreach (var deletedMaterialKey in deletedMaterialList)
                    {
                        oldParts.materialList.Remove(oldParts.materialList.Find(x =>
                            x.materialKey.Equals(deletedMaterialKey)));
                    }
                }
            }

            foreach (var deletedPartsKey in deletedPartsList)
            {
                oldCategory.partsList.Remove(oldCategory.partsList.Find(x =>
                    x.partsKey.Equals(deletedPartsKey)));
            }
        }
    }

    // 컨버트 결과를 출력합니다
    private void FinishProcess()
    {
        StringBuilder sb = new StringBuilder();
        if (newCategoryList.Count > 0)
        {
            sb.Clear();
            sb.Append("New Category\n");
            foreach (var newCategoryKey in newCategoryList)
            {
                sb.Append(newCategoryKey).Append("\n");
            }
            Debug.Log(sb.ToString());
            newCategoryList.Clear();
        }

        if (deletedCategoryList.Count > 0)
        {
            sb.Clear();
            sb.Append("Deleted Category\n");
            foreach (var deletedCategoryKey in deletedCategoryList)
            {
                sb.Append(deletedCategoryKey).Append("\n");
            }
            Debug.Log(sb.ToString());
            deletedCategoryList.Clear();
        }

        if (newItemList.Count > 0)
        {
            sb.Clear();
            sb.Append("New Item\n");
            foreach (var newItemKey in newItemList)
            {
                sb.Append(newItemKey).Append("\n");
            }
            Debug.Log(sb.ToString());
            newItemList.Clear();
        }

        if (deletedItemList.Count > 0)
        {
            sb.Clear();
            sb.Append("Deleted Item\n");
            foreach (var deletedItemKey in deletedItemList)
            {
                sb.Append(deletedItemKey).Append("\n");
            }
            Debug.Log(sb.ToString());
            deletedItemList.Clear();
        }

        if (newPartsList.Count > 0)
        {
            sb.Clear();
            sb.Append("New Parts\n");
            foreach (var newPartsKey in newPartsList)
            {
                sb.Append(newPartsKey).Append("\n");
            }
            Debug.Log(sb.ToString());
            newPartsList.Clear();
        }

        if (deletedPartsList.Count > 0)
        {
            sb.Clear();
            sb.Append("Deleted Parts\n");
            foreach (var deletedPartsKey in deletedPartsList)
            {
                sb.Append(deletedPartsKey).Append("\n");
            }
            Debug.Log(sb.ToString());
            deletedPartsList.Clear();
        }

        if (newMaterialList.Count > 0)
        {
            sb.Clear();
            sb.Append("New Materials\n");
            foreach (var newMaterialKey in newMaterialList)
            {
                sb.Append(newMaterialKey).Append("\n");
            }
            Debug.Log(sb.ToString());
            newMaterialList.Clear();
        }

        if (deletedMaterialList.Count > 0)
        {
            sb.Clear();
            sb.Append("Deleted Materials\n");
            foreach (var deletedMaterialKey in deletedMaterialList)
            {
                sb.Append(deletedMaterialKey).Append("\n");
            }
            Debug.Log(sb.ToString());
            deletedMaterialList.Clear();
        }

        AssetDatabase.Refresh();

        Debug.Log($"Copy Time : {Time.realtimeSinceStartup - timer} sec");
    }

    private void CopyThumbnailFile(Texture target, string itemKey)
    {
        string[] spl = AssetDatabase.GetAssetPath(target).Split('/', '\\');
        string fileName = spl[spl.Length - 1];
        string path = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(target);
        string dstDir = Application.dataPath + $"/../ReleaseList{System.DateTime.Now.Month:00}{System.DateTime.Now.Day:00}/";
        if (!System.IO.Directory.Exists(dstDir)) System.IO.Directory.CreateDirectory(dstDir);
        string dst = dstDir + fileName;
        if (System.IO.File.Exists(dst)) System.IO.File.Delete(dst);
        FileUtil.CopyFileOrDirectory(path, dst);

        Texture2D loadTexture = LoadTextureFile(dst);
        string resizedFilePath = dstDir + "Thumbnail_" + itemKey + ".png";
        TextureResizeTo256(loadTexture, dst, resizedFilePath);
    }
    public Texture2D LoadTextureFile(string inputFilePath)
    {
        byte[] fileData = File.ReadAllBytes(inputFilePath);

        Texture2D loadTexture = new Texture2D(1, 1);
        loadTexture.LoadImage(fileData);
        loadTexture.Apply();

        return loadTexture;
    }

    public void TextureResizeTo256(Texture2D texture, string outputFilePath, string resizedFilePath)
    {
        Texture2D resizeTexture = new Texture2D(256, 256);
        Color[] colors = texture.GetPixels(2);
        if (colors.Length < 256 * 256)
        {
            colors = texture.GetPixels();
        }

        resizeTexture.SetPixels(colors);
        resizeTexture.Apply();

        byte[] bytes = resizeTexture.EncodeToPNG();
        File.Delete(outputFilePath);
        File.WriteAllBytes(resizedFilePath, bytes);
    }
    private void WriteJsonFile(CategoryComparatorItem list)
    {
        string cont = JsonUtility.ToJson(list, true);
        string dstDir = Application.dataPath + $"/../ReleaseList{System.DateTime.Now.Month:00}{System.DateTime.Now.Day:00}/";
        if (!System.IO.Directory.Exists(dstDir)) System.IO.Directory.CreateDirectory(dstDir);
        string dst = dstDir + $"ReleaseList_{System.DateTime.Now.Month:00}{System.DateTime.Now.Day:00}.json";
        if (System.IO.File.Exists(dst)) System.IO.File.Delete(dst);
        System.IO.File.WriteAllText(dst, cont);
    }

    [ContextMenu("CombineTable")]
    private void CombineItemAndParts()
    {
        string addedTableCategoryKey;
        int categoryIndex;

        for (int i = 0; i < addedTable.categoryList.Count; i++)
        {
            addedTableCategoryKey = addedTable.categoryList[i].categoryKey;
            var categoryItem = baseTable.categoryList.Find(x => x.categoryKey == addedTableCategoryKey);

            if (categoryItem != null)
            {
                categoryIndex = baseTable.categoryList.FindIndex(x => x.categoryKey == addedTableCategoryKey);

                for (int j = 0; j < addedTable.categoryList[i].itemList.Count; j++)
                {
                    var addedTableDefaultItem = addedTable.categoryList[i].itemList[j];
                    var defaultItem = categoryItem.itemList.Find(x => x.itemKey == addedTableDefaultItem.itemKey);

                    if (defaultItem == null)
                    {
                        addedItemList.Add(addedTableDefaultItem.itemKey);
                    }
                    else
                    {
                        overlapItemList.Add(addedTableDefaultItem.itemKey);
                        baseTable.categoryList[categoryIndex].itemList.Remove(defaultItem);
                    }

                    baseTable.categoryList[categoryIndex].itemList.Add(addedTableDefaultItem);
                }

                for (int j = 0; j < addedTable.categoryList[i].partsList.Count; j++)
                {
                    var addedTablePartsItem = addedTable.categoryList[i].partsList[j];
                    var partsItem = categoryItem.partsList.Find(x => x.partsKey == addedTablePartsItem.partsKey);

                    if (partsItem == null)
                    {
                        addedPartsList.Add(addedTablePartsItem.partsKey);
                    }
                    else
                    {
                        overlapPartsList.Add(addedTablePartsItem.partsKey);
                        baseTable.categoryList[categoryIndex].partsList.Remove(partsItem);
                    }

                    baseTable.categoryList[categoryIndex].partsList.Add(addedTablePartsItem);
                }
            }

            else
            {
                addedCategoryList.Add(addedTableCategoryKey);
                baseTable.categoryList.Add(addedTable.categoryList[i]);
            }

        }
        UpdatedTableLog();
    }

    private void UpdatedTableLog()
    {
        StringBuilder stringBuilder = new StringBuilder();

        if (addedCategoryList.Count > 0)
        {
            stringBuilder.Clear();
            stringBuilder.Append("###### 추가된 카테고리 개수 : ").Append(addedCategoryList.Count).Append(" ###### \n");

            foreach (string addedCategory in addedCategoryList)
            {
                stringBuilder.Append("추가된 카테고리 : ").Append(addedCategory).Append("\n");
            }

            Debug.Log(stringBuilder.ToString());
            addedCategoryList.Clear();
        }

        if (addedItemList.Count > 0)
        {
            stringBuilder.Clear();
            stringBuilder.Append("###### 추가된 아이템 개수 : ").Append(addedItemList.Count).Append(" ###### \n");

            foreach (string addedItem in addedItemList)
            {
                stringBuilder.Append("추가된 아이템 : ").Append(addedItem).Append("\n");
            }

            Debug.Log(stringBuilder.ToString());
            addedItemList.Clear();
        }

        if (overlapItemList.Count > 0)
        {
            stringBuilder.Clear();
            stringBuilder.Append("###### 중복으로 인한 교체된 아이템 개수 : ").Append(overlapItemList.Count).Append(" ###### \n");

            foreach (string overlapItem in overlapItemList)
            {
                stringBuilder.Append("교체된 아이템 : ").Append(overlapItem).Append("\n");
            }

            Debug.Log(stringBuilder.ToString());
            overlapItemList.Clear();
        }

        if (addedPartsList.Count > 0)
        {
            stringBuilder.Clear();
            stringBuilder.Append("###### 추가된 파츠 개수 : ").Append(addedPartsList.Count).Append(" ###### \n");

            foreach (string addedPartsItem in addedPartsList)
            {
                stringBuilder.Append("추가된 파츠 : ").Append(addedPartsItem).Append("\n");
            }

            Debug.Log(stringBuilder.ToString());
            addedPartsList.Clear();
        }

        if (overlapPartsList.Count > 0)
        {
            stringBuilder.Clear();
            stringBuilder.Append("###### 중복으로 인한 교체된 파츠 개수 : ").Append(overlapPartsList.Count).Append(" ###### \n");

            foreach (string overlapPartsItem in overlapPartsList)
            {
                stringBuilder.Append("교체된 파츠 : ").Append(overlapPartsItem).Append("\n");
            }

            Debug.Log(stringBuilder.ToString());
            overlapPartsList.Clear();
        }
    }

}

#endif