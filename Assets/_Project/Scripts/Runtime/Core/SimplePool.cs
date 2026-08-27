using System;
using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Pool simples de componentes: zero alocação em regime.
    /// Requisito mobile desde o dia 1 — inimigos e projéteis nunca são destruídos, só reciclados.
    /// </summary>
    public class SimplePool<T> where T : Component
    {
        readonly Stack<T> _stack = new Stack<T>();
        readonly Func<T> _factory;

        public SimplePool(Func<T> factory) => _factory = factory;

        public T Get()
        {
            // guarda contra objetos destruídos (ex.: troca de cena)
            while (_stack.Count > 0)
            {
                var item = _stack.Pop();
                if (item != null)
                {
                    item.gameObject.SetActive(true);
                    return item;
                }
            }
            return _factory();
        }

        public void Release(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            _stack.Push(item);
        }
    }
}
