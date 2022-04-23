﻿/*
 *  This code is heavily adapted from the Myra UndoRedoStack (https://github.com/rds1983/Myra/blob/a9dbf7a1ceedc19f9e416c754eaf38e89a89a746/src/Myra/Graphics2D/UI/TextEdit/UndoRedoStack.cs)
 *
 *  MIT License
 *
 *  Copyright (c) 2017-2020 The Myra Team
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.

 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

using System.Collections.Generic;

namespace Blish_HUD.Controls; 

internal class UndoRedoStack {

    private readonly Stack<UndoRedoRecord> _stack = new();

    public void Reset() {
        _stack.Clear();
    }

    public UndoRedoRecord Pop() {
        if (_stack.Count == 0) return null;

        return _stack.Pop();
    }

    public void MakeInsert(int where, int length) {
        if (length <= 0) return;

        _stack.Push(new UndoRedoRecord() {
            OperationType = OperationType.Insert,
            Index         = where,
            Length        = length
        });
    }

    public void MakeDelete(string text, int where, int length) {
        if (length <= 0) return;

        _stack.Push(new UndoRedoRecord() {
            OperationType = OperationType.Delete,
            Index         = where,
            Length        = length,
            Data          = text.Substring(where, length)
        });
    }

    public void MakeReplace(string text, int where, int length, int newLength) {
        if (length <= 0) return;

        _stack.Push(new UndoRedoRecord() {
            OperationType = OperationType.Replace,
            Index         = where,
            Length        = newLength,
            Data          = text.Substring(where, length)
        });
    }

}