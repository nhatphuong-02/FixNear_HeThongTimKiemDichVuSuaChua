
package com.example.fixnear.helper

import android.text.InputType
import android.widget.EditText
import android.widget.ImageView
import com.example.fixnear.R

fun setupPasswordToggle(editText: EditText, toggleButton: ImageView) {
    var isVisible = false

    toggleButton.setOnClickListener {
        val cursorPosition = editText.selectionStart
        isVisible = !isVisible

        editText.inputType = if (isVisible) {
            InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD
        } else {
            InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PASSWORD
        }

        editText.setSelection(cursorPosition.coerceIn(0, editText.text.length))

        toggleButton.setImageResource(
            if (isVisible) R.drawable.ic_eye_off
            else R.drawable.ic_eye
        )

        toggleButton.contentDescription =
            if (isVisible) "Ẩn mật khẩu"
            else "Hiển thị mật khẩu"
    }
}
