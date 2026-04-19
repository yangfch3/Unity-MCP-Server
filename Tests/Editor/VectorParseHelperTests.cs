using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// VectorParseHelper 单元测试。
    /// </summary>
    public class VectorParseHelperTests
    {
        // ── ParseVector2 ──

        [Test]
        public void ParseVector2_ValidList_ReturnsVector2()
        {
            var input = new List<object> { 1.0, 2.0 };
            var result = VectorParseHelper.ParseVector2(input);
            Assert.AreEqual(1f, result.x, 0.0001f);
            Assert.AreEqual(2f, result.y, 0.0001f);
        }

        [Test]
        public void ParseVector2_InsufficientElements_ThrowsArgumentException()
        {
            var input = new List<object> { 1.0 };
            Assert.Throws<ArgumentException>(() => VectorParseHelper.ParseVector2(input));
        }

        [Test]
        public void ParseVector2_NotAList_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => VectorParseHelper.ParseVector2("not a list"));
        }

        // ── ParseVector3 ──

        [Test]
        public void ParseVector3_ValidList_ReturnsVector3()
        {
            var input = new List<object> { 1.0, 2.0, 3.0 };
            var result = VectorParseHelper.ParseVector3(input);
            Assert.AreEqual(1f, result.x, 0.0001f);
            Assert.AreEqual(2f, result.y, 0.0001f);
            Assert.AreEqual(3f, result.z, 0.0001f);
        }

        [Test]
        public void ParseVector3_InsufficientElements_ThrowsArgumentException()
        {
            var input = new List<object> { 1.0, 2.0 };
            Assert.Throws<ArgumentException>(() => VectorParseHelper.ParseVector3(input));
        }

        // ── ParseVector4 ──

        [Test]
        public void ParseVector4_ValidList_ReturnsVector4()
        {
            var input = new List<object> { 1.0, 2.0, 3.0, 4.0 };
            var result = VectorParseHelper.ParseVector4(input);
            Assert.AreEqual(1f, result.x, 0.0001f);
            Assert.AreEqual(2f, result.y, 0.0001f);
            Assert.AreEqual(3f, result.z, 0.0001f);
            Assert.AreEqual(4f, result.w, 0.0001f);
        }

        // ── ParseColor ──

        [Test]
        public void ParseColor_ValidList_ReturnsColor()
        {
            var input = new List<object> { 0.5, 0.6, 0.7, 1.0 };
            var result = VectorParseHelper.ParseColor(input);
            Assert.AreEqual(0.5f, result.r, 0.0001f);
            Assert.AreEqual(0.6f, result.g, 0.0001f);
            Assert.AreEqual(0.7f, result.b, 0.0001f);
            Assert.AreEqual(1f, result.a, 0.0001f);
        }

        [Test]
        public void ParseColor_InsufficientElements_ThrowsArgumentException()
        {
            var input = new List<object> { 0.5, 0.6 };
            Assert.Throws<ArgumentException>(() => VectorParseHelper.ParseColor(input));
        }

        // ── ParseRect ──

        [Test]
        public void ParseRect_ValidList_ReturnsRect()
        {
            var input = new List<object> { 10.0, 20.0, 100.0, 50.0 };
            var result = VectorParseHelper.ParseRect(input);
            Assert.AreEqual(10f, result.x, 0.0001f);
            Assert.AreEqual(20f, result.y, 0.0001f);
            Assert.AreEqual(100f, result.width, 0.0001f);
            Assert.AreEqual(50f, result.height, 0.0001f);
        }

        // ── ToFloat ──

        [Test]
        public void ToFloat_Long_ReturnsFloat()
        {
            var result = VectorParseHelper.ToFloat((long)42);
            Assert.AreEqual(42f, result, 0.0001f);
        }

        [Test]
        public void ToFloat_Double_ReturnsFloat()
        {
            var result = VectorParseHelper.ToFloat((double)3.14);
            Assert.AreEqual(3.14f, result, 0.0001f);
        }

        [Test]
        public void ToFloat_Int_ReturnsFloat()
        {
            var result = VectorParseHelper.ToFloat((int)7);
            Assert.AreEqual(7f, result, 0.0001f);
        }

        [Test]
        public void ToFloat_InvalidType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => VectorParseHelper.ToFloat("not a number"));
        }

        // ── MiniJson 兼容性 ──

        [Test]
        public void ParseVector3_LongElements_ReturnsVector3()
        {
            // MiniJson 将整数解析为 long
            var input = new List<object> { (long)1, (long)2, (long)3 };
            var result = VectorParseHelper.ParseVector3(input);
            Assert.AreEqual(1f, result.x, 0.0001f);
            Assert.AreEqual(2f, result.y, 0.0001f);
            Assert.AreEqual(3f, result.z, 0.0001f);
        }
    }
}
