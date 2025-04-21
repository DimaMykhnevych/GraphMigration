import { Component, Input, OnInit } from '@angular/core';
import { ScaleType } from '@swimlane/ngx-charts';
import { ChartDataItem } from 'src/app/models/chart-data-item';

@Component({
  selector: 'app-bar-chart',
  templateUrl: './bar-chart.component.html',
  styleUrls: ['./bar-chart.component.scss'],
})
export class BarChartComponent implements OnInit {
  @Input() public data: ChartDataItem[] = [];
  @Input() public xAxisLabel: string = 'x';
  @Input() public yAxisLabel: string = 'y';

  // options
  public showXAxis = true;
  public showYAxis = true;
  public gradient = false;
  public showLegend = false;
  public showXAxisLabel = true;
  public showYAxisLabel = true;

  public colorScheme = {
    domain: ['#a8385d', '#7aa3e5'],
    name: '',
    selectable: false,
    group: ScaleType.Linear,
  };

  constructor() {}

  ngOnInit(): void {}
}
