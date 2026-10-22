using System.Collections;
using System.Linq;
using UnityEngine;

public class CoroutineTest : MonoBehaviour
{
    // 매 초마다 "시작!" -> "1초!" -> "2초" -> "3초"를 센 다음 "나로호 발사!"라는 로그를 출력하고 싶다.
    // 유니티의 코루틴이란

    // 동기 vs 비동기
    // 동기 : 순서대로 진행
    // 비동기 : 병렬 진행

    // 비동기 실행과 제어를 효율적으로 제공하는 기능이다.
    // 1. 비동기 실행
    // 2. 시간/프레임에 따른 실행 제어
    // - IEnumerator : 코루틴 함수의 반환형
    // - StartCoroutine(함수()) : 코루틴 함수를 실행하는 방법
    // - yield : 정해진 시간/프레임동안 쉼
    // - yield return null : 한 프레임만 쉼

    private void Start()
    {
        Debug.Log("시작!");
        StartCoroutine(CountDown_Coroutine());

        Debug.Log("끝!");
    }

    private IEnumerator CountDown_Coroutine()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("1초!");

        yield return new WaitForSeconds(1f);
        Debug.Log("2초!");

        yield return new WaitForSeconds(1f);
        Debug.Log("3초!");

        yield return new WaitForSeconds(1f);
        Debug.Log("나로호 발사!");
    }
}
