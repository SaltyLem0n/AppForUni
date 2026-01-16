# Coupon Layout Change Request

## Objective
Change the coupon print layout from **horizontal (left-to-right)** ordering to **vertical (top-to-bottom in columns)** ordering.

## Current Behavior
Coupons are currently displayed in a row-first order:
```
1  2
3  4
5  6
7  8
9  10
```

## Desired Behavior
Coupons should display in a column-first order:
```
1  6
2  7
3  8
4  9
5  10
```

The numbering should go down the **left column first** (1, 2, 3, 4, 5), then continue down the **right column** (6, 7, 8, 9, 10).

## Implementation

### Files to Modify

1. **`AppForUni/Views/Employees/Coupons.cshtml`**
   - Keep the simple `@foreach` loop (no complex interleaving logic needed)
   - Let CSS handle the layout

2. **`AppForUni/wwwroot/css/coupons.css`**
   - Change `.sheet` flexbox from row-based to column-based layout
   - Add `flex-direction: column` and `flex-wrap: wrap`
   - Set a `max-height: 297mm` to constrain the column height

### CSS Changes Required

In `coupons.css`, update the `.sheet` class:

```css
/* --- SHEET --- */
.sheet {
    background: white;
    width: 210mm;
    min-height: 297mm;
    max-height: 297mm;          /* ADD THIS */
    padding: 0;
    box-shadow: 0 0 20px rgba(0, 0, 0, 0.5);
    margin-bottom: 20px;
    display: flex;
    flex-direction: column;      /* ADD THIS */
    flex-wrap: wrap;
    align-content: flex-start;
}
```

And in the `@media print` section:

```css
@media print {
    /* ... other styles ... */
    
    .sheet {
        width: 100%;
        max-height: 297mm;          /* CHANGE from height: 100% */
        box-shadow: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;      /* ADD THIS */
        flex-wrap: wrap;
        align-content: flex-start;
    }
}
```

## How It Works
- By setting `flex-direction: column`, items flow **vertically** (top to bottom)
- By setting `max-height: 297mm` (A4 height), the column is constrained
- By setting `flex-wrap: wrap`, when the column reaches the height limit, items wrap to a **new column** (to the right)
- Each coupon is `56mm` tall, so 5 coupons fit in one column before wrapping to the next column

This creates the desired vertical ordering without needing complex Razor logic.
