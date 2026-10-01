import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';

@Component({
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './auth-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPageComponent {
  readonly auth = inject(AuthService);
  readonly language = inject(LanguageService);
  private readonly router = inject(Router);
  readonly registering = inject(ActivatedRoute).snapshot.data['register'] === true;
  readonly pending = signal(false);
  readonly showPassword = signal(false);
  readonly error = signal<string | null>(null);
  readonly backendError = signal<string | null>(null);
  private readonly fb = inject(FormBuilder);
  readonly form = this.fb.nonNullable.group(
    {
      dealershipName: [
        '',
        this.registering
          ? [Validators.required, Validators.maxLength(200), Validators.pattern(/\S/)]
          : [],
      ],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      password: [
        '',
        [
          Validators.required,
          Validators.maxLength(256),
          ...(this.registering ? [Validators.minLength(12)] : []),
        ],
      ],
      confirmPassword: ['', this.registering ? [Validators.required] : []],
    },
    {
      validators: (group) =>
        this.registering && group.get('password')?.value !== group.get('confirmPassword')?.value
          ? { mismatch: true }
          : null,
    },
  );
  invalid(field: 'email' | 'password' | 'confirmPassword' | 'dealershipName') {
    const control = this.form.controls[field];
    return control.touched && control.invalid;
  }
  submit() {
    if (this.pending() || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.pending.set(true);
    this.error.set(null);
    this.backendError.set(null);
    const value = this.form.getRawValue();
    const request = this.registering
      ? this.auth.register({
          ...value,
          email: value.email.trim(),
          dealershipName: value.dealershipName.trim(),
        })
      : this.auth.login({ email: value.email.trim(), password: value.password });
    request.pipe(finalize(() => this.pending.set(false))).subscribe({
      next: (user) => {
        if (user) {
          this.form.reset();
          void this.router.navigateByUrl('/dashboard');
        } else this.error.set('auth.errors.generic');
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(
          e.status === 401 && !this.registering
            ? 'auth.login.invalidCredentials'
            : e.status === 409 && this.registering
              ? 'auth.register.duplicate'
              : 'auth.errors.generic',
        );
        if (e.status === 400 && this.registering) {
          const detail = e.error?.detail;
          if (typeof detail === 'string') this.backendError.set(detail);
        }
      },
    });
  }
}
