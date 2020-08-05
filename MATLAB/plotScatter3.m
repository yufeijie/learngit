function plotScatter3(titleName, xLabelName, yLabelName, zLabelName, len, x, y, z, xBest, yBest, zBest)
% plotScatter3 画三维散点图
% titleName 标题名
% xLabelName yLabelName zLabelName 坐标轴名
% len 数据数
% x y z 散点数据
% xBest yBest zBest 最优数据
figure(6)
if len > 0
scatter3(x(1:len), y(1:len), z(1:len), 10);
hold on
end
scatter3(xBest, yBest, zBest, 20, 'r', 'filled');
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end

