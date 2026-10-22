using UnityEngine;
using System.Collections.Generic;

public class DictionaryTest : MonoBehaviour
{
    // 배열이나 List는 인덱스를 이용해 요소에 접근
    // 딕셔너리는 요소를 저장할 때 "키"와 "값"을 쌍으로 저장한다.
    // HashTable(=Dictionary)는 키와 값이 쌍으로 이루어진 데이터를 관리(CRUDS)할 때 사용하는 자료구조

    private void Start()
    {
        var ages = new List<int> { 22, 21, 20, 35 };

        var students = new Dictionary<string, int> { { "AA", 22 }, { "BB", 21 }, { "CC", 20 } };

        Debug.Log(students["AA"]); // 값 조회

        students["BB"] = 20; // 특정 키의 값 수정
        students.Add("DD", 17);
        students.Remove("AA");
        var hasA = students.ContainsKey("AA");

        foreach (KeyValuePair<string, int> item in students)
        {
            Debug.Log($"{item.Key}의 나이: {item.Value}");
        }
    }
}
