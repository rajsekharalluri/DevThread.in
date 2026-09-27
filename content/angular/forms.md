---
id: angular-forms
slug: forms
title: "Angular Forms: Reactive Forms, Validation, and Custom Controls"
category: angular
categoryTitle: Angular
difficulty: intermediate
estimatedMinutes: 55
version:
  minimum: "Angular 17+"
prerequisites: [angular-fundamentals, angular-dependency-injection]
tags: [angular, forms, reactive-forms, validation, forms-api]
relatedTopics: [angular-rxjs-http, angular-routing]
order: 50
status: published
---
# Angular Forms: Reactive Forms, Validation, and Custom Controls

## Introduction
Angular forms track values, validity, interaction state, and submission. Reactive forms define the form model in TypeScript; template-driven forms infer it from the template.

```text
User input
   ↓
FormControl value
   ↓ validators
valid/invalid + touched/dirty/pending
   ↓
submit only valid data
```

## Purpose
Forms need consistent validation, error display, dynamic fields, async checks, and safe submission. Reactive forms make those rules explicit and testable.

## Real-World Simple Example
```typescript
readonly form = this.fb.group({
  email: ['', [Validators.required, Validators.email]],
  quantity: [1, [Validators.required, Validators.min(1)]]
});

submit(): void {
  if (this.form.invalid) {
    this.form.markAllAsTouched();
    return;
  }
  this.orders.create(this.form.getRawValue());
}
```

A template can show errors only after a control is touched or the user attempts submission:

```html
@if (form.controls.email.invalid && form.controls.email.touched) {
  <p>A valid email is required.</p>
}
```

## Professional company-level example
Use reactive forms for checkout, administration, and dynamic business workflows. Add async validation with debounce and cancellation; map server-side validation errors back to controls; never treat disabled buttons as security.

```typescript
emailValidator(): AsyncValidatorFn {
  return control => this.customers.isAvailable(control.value).pipe(
    debounceTime(300),
    map(available => available ? null : { emailTaken: true }),
    catchError(() => of({ validationUnavailable: true }))
  );
}
```

For a custom reusable input such as money/date/address, implement `ControlValueAccessor` so the control participates in form value, touched, disabled, and validation behavior rather than inventing a second form API.

## Important behavior

```text
setValue      → requires the complete form shape
patchValue    → updates a partial shape
setErrors     → adds server/business error
markAllAsTouched → makes hidden validation visible
valueChanges  → Observable of value changes
statusChanges → Observable of validation status
```

Client validation improves UX. The server must validate again because a user can bypass Angular entirely and call the API directly.

## Comparison
| Choice | Strength | Limitation |
|---|---|---|
| Reactive forms | Explicit, testable, dynamic | More TypeScript setup |
| Template-driven | Fast for tiny forms | Implicit/less testable complex state |
| Sync validator | Immediate local rule | Cannot check server state |
| Async validator | Server-backed rule | Needs debounce/cancellation |
| `setValue` | Enforces full shape | Verbose for partial updates |
| `patchValue` | Convenient partial update | Can silently omit a field |

## Interview Questions
- **[L1]** What is the difference between reactive and template-driven forms?
- **[L1]** What do `touched`, `dirty`, and `invalid` mean?
- **[L2]** Why are reactive forms easier to test?
- **[L2]** How should async validators handle rapid typing?
- **[L3]** Why must server validation still exist?
- **[L3]** What is `ControlValueAccessor` used for?

## Interview Answers
1. Reactive forms define controls/validators in TypeScript; template-driven forms infer them through template directives. Reactive forms scale better for dynamic/complex workflows.
2. `touched` means the control has received/left focus, `dirty` means its value changed, and `invalid` means one or more validators returned errors.
3. The form model is a normal TypeScript object graph, so validators and state transitions can be tested without rendering a browser template.
4. Debounce input and cancel stale server requests with `switchMap`; otherwise every keystroke creates traffic and older responses can overwrite newer results.
5. Client JavaScript can be modified or bypassed. The API is the authoritative security/data-integrity boundary.
6. It allows a custom component to behave like a native Angular form control, participating in value, disabled, touched, change, and validation flows.

## Expert perspective
A form is a contract between user experience and backend validation. Senior engineers make error mapping, accessibility, cancellation, server validation, and partial-save behavior explicit rather than treating the submit button as the validation boundary.
