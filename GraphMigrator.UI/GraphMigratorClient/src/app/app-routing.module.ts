import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { QueryComparisonComponent } from './components/query-comparison/query-comparison.component';
import { NavbarComponent } from './components/navbar/navbar.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'query-comparison',
    pathMatch: 'full',
  },
  {
    path: '',
    component: NavbarComponent,
    children: [
      {
        path: 'query-comparison',
        component: QueryComparisonComponent,
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'query-comparison',
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
