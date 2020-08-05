function plotParetoAnalysis(headline, subTitle1, subTitle2, subTitle3, subTitle4, xLabelName, yLabelName, zLabelName, dataPareto)
% plotPareto 画Pareto图
% headline 大标题
% subTitle1 2 3 4 子标题
% xLabelName yLabelName zLabelName 坐标轴名
% size 数据总数
% data  数据
% dataPareto Pareto最优数据
sz = 12;
xData = dataPareto(:, 1);
yData = dataPareto(:, 2);
zData = dataPareto(:, 3);
N1 = dataPareto(:, 4);
N2 = dataPareto(:, 5);

figure(7)

subplot(3,1,1);
scatter(N1, N2, sz, getCmap(zData, 1, 95, 100), 'filled');
grid on
title(subTitle1)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)

subplot(3,1,2);
scatter(N1, zData, sz, 'filled');

subplot(3,1,3);
scatter(N2, zData, sz, 'filled');


end

function cmap = getCmap(c, type, min, max)
shift = 0.4;

n = length(c);
cmap = zeros(n, 3);
for i = 1 : n
    if type == 1
        normal = (c(i)-min)/(max-min);
        delta = (1-shift)*normal;
        cmap(i, 1) = delta;
        cmap(i, 2) = delta;
        cmap(i, 3) = shift+delta;
    else
        normal = (c(i)-min)/(max-min);
        delta = (1-shift)*normal;
        cmap(i, 1) = delta;
        cmap(i, 2) = shift+delta;
        cmap(i, 3) = delta;
    end
end
end