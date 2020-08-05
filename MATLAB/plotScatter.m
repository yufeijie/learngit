function plotScatter(titleName, xLabelName, yLabelName, len, x, y, xBest, yBest)
% plotScatter 画散点图
% titleName 标题名
% xLabelName yLabelName zLabelName 坐标轴名
% len 数据数
% x y 散点数据
% xBest yBest 最优数据
figure(5)
if len > 0
scatter(x(1:len), y(1:len), 10);
hold on
end
scatter(xBest, yBest, 20, 'r', 'filled');
hold on
plot(xBest, yBest, 'r');
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
end

