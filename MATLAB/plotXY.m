function plotXY(titleName, xLabelName, yLabelName, x, y)
% plotXYZ 画三维曲线图
% titleName 标题名
% xLabelName X轴名
% yLabelName Y轴名
% zLabelName Z轴名
% x y 数据
figure(1)
plot(x, y)
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
end