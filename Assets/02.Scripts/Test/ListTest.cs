using UnityEngine;
using System.Collections.Generic;

public class ListTest : MonoBehaviour
{
    // 자료구조 (Data Structure)
    // 이런 데이터들을 "효율적"으로 관리(CRUDS)하기 위해 저장하는 구조
    // C - Create : 생성
    // R - Read : 읽기
    // U - Update : 수정
    // D - Delete : 삭제
    // S - Search : 검색

    // 선형 자료구조 - 배열, 리스트, 큐, 스택
    // 비선형 자료구조 - 트리, 그래프, 해쉬테이블 ...

    private void Start()
    {
        var ages = new int[] { 22, 21, 20, 35 }; // 배열의 선언과 정의

        // 1. 배열은 선언할 때 지정한 배열의 크길르 변경할 수 없다.
        // -> 배열에 데이터를 추가하거나 삭제하려면 새로운 배열을 생성해야 한다.
        // -> 배열은 데이터를 중간에 추가하거나 삭제하는 경우 비효율적인 과정이 필요하다.

        /*
         ages[0] = 17;
        ages[1] = 40;
        ages[2] = 27;
        ages[3] = 33;
        ages[4] = 42; // 범위를 벗어났기 때문에 IndexOutOfRange 에러가 뜬다.
        */

        // 배열의 단점을 보완하기 위해 자료구조는 동적 배열 혹은 연결 리스트를 사용
        // C#은 이것을 "콜렉션" 네임스페이스 안에 List로 구현해놨음.
        // 콜렉션(Collection) : 컴퓨터공학의 자료구조를 C#에서 구조화하여 클래스로 구현해 놓은 것들

        // List -> 배열과 똑같은데 필요에 따라 크기를 동적으로 조절 가능한 배열
        //

        var ages2 = new List<int> { 22, 21, 20, 35 };
        ages2[0] = 17;
        ages2[1] = 40;
        ages2[2] = 27;
        ages2[3] = 42;

        ages2.Add(33); // 데이터 끝에 추가
        ages2.Insert(1, 12); // 데이터를 중간에 삽입
        ages2.Remove(42); // 제거할 데이터의 값을 매개변수로 전달해서 삭제
        ages2.RemoveAt(3); // 제거할 데이터의 위치를 전달해서 삭제


        for (var i = 0; i < ages2.Count; i++)
        {
            Debug.Log(ages2[i]); // 인덱스를 통한 조회
        }

        foreach (var age in ages2)
        {
            Debug.Log(age); // foreach를 통한 조회
        }
    }
}
