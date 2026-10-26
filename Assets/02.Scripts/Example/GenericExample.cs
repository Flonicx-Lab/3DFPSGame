using UnityEngine;

// 제네릭(generic: 일반화)는 "타입 (자료형)에 상관없는~"
// -> 이 자료형을 사용할 때 자료형이 무엇인지 정의하지 않고 그때그때 쓰는것
// -> Cup of Drink

public class Water
{
}

public class Coke
{
}

public class Cup<T>
{
    public T Drink; // 명사

    public Cup(T Drink)
    {
        this.Drink = Drink;
    }
}

public class GenericExample : MonoBehaviour
{
    private void Start()
    {
        var cup1 = new Cup<int>(17);
        var cup2 = new Cup<float>(0.2f);
        var cup3 = new Cup<string>("콜라");
        var cup4 = new Cup<bool>(true);
        var cup5 = new Cup<Water>(new Water());
        var cup6 = new Cup<Coke>(new Coke());
    }

    private void Update()
    {
    }
}
