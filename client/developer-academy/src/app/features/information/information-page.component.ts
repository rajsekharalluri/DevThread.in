import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({ standalone: true, imports: [RouterLink], templateUrl: './information-page.component.html' })
export class InformationPageComponent {
  readonly page = inject(ActivatedRoute).snapshot.data['page'] as string;

  get title(): string {
    return ({
      about: 'About DevThread',
      contact: 'Contact',
      privacy: 'Privacy Policy',
      terms: 'Terms of Use',
      cookies: 'Cookies and Analytics'
    } as Record<string, string>)[this.page] ?? 'DevThread';
  }
}
